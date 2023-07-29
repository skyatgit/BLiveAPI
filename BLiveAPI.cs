using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrotliSharpLib;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BLiveAPI;

/// <summary>
///     B站直播间弹幕接口
/// </summary>
public class BLiveApi : BLiveEvents
{
    private const string WsHost = "wss://broadcastlv.chat.bilibili.com/sub";
    private ClientWebSocket _clientWebSocket;
    private ulong? _roomId;
    private ulong? _uid;
    private CancellationTokenSource _webSocketCancelToken;


    private static byte[] GetChildFromProtoData(byte[] protoData, int target)
    {
        using (var input = new CodedInputStream(protoData))
        {
            while (!input.IsAtEnd)
            {
                var tag = input.ReadTag();
                var tagId = WireFormat.GetTagFieldNumber(tag);
                if (tagId == target) return input.ReadBytes().ToByteArray();
                input.SkipLastField();
            }
        }

        return Array.Empty<byte>();
    }

    private void DecodeSms(JObject sms)
    {
        var cmd = (string)sms.GetValue("cmd");
        OnOpSendSmsReply(cmd, sms);
        switch (cmd)
        {
            case "DANMU_MSG":
            {
                var msg = (string)sms["info"][1];
                var userId = (long)sms["info"][2]?[0];
                var userName = (string)sms["info"][2]?[1];
                var protoData = Convert.FromBase64String(sms["dm_v2"].ToString());
                var face = Encoding.UTF8.GetString(GetChildFromProtoData(GetChildFromProtoData(protoData, 20), 4));
                OnDanmuMsg(msg, userId, userName, face, sms);
                break;
            }
            default:
            {
                OnOtherMessages(cmd, sms);
                break;
            }
        }
    }

    private static int BytesToInt(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes.Length switch
        {
            2 => BitConverter.ToInt16(bytes, 0),
            4 => BitConverter.ToInt32(bytes, 0),
            _ => throw new InvalidBytesLengthException()
        };
    }

    private void DecodeMessage(ServerOperation operation, byte[] messageData)
    {
        switch (operation)
        {
            case ServerOperation.OpAuthReply:
                OnOpAuthReply((JObject)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(messageData)), messageData);
                break;
            case ServerOperation.OpHeartbeatReply:
                OnOpHeartbeatReply(BytesToInt(messageData), messageData);
                break;
            case ServerOperation.OpSendSmsReply:
                DecodeSms((JObject)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(messageData)));
                break;
            default:
                throw new UnknownServerOperationException(operation);
        }
    }

    private void DecodePacket(byte[] packetData)
    {
        while (true)
        {
            var header = new ArraySegment<byte>(packetData, 0, 16).ToArray();
            var body = new ArraySegment<byte>(packetData, 16, packetData.Length - 16).ToArray();
            var version = BytesToInt(new ArraySegment<byte>(header, 6, 2).ToArray());
            switch (version)
            {
                case 0:
                case 1:
                    var firstPacketLength = BytesToInt(new ArraySegment<byte>(header, 0, 4).ToArray());
                    var operation = (ServerOperation)BytesToInt(new ArraySegment<byte>(header, 8, 4).ToArray());
                    DecodeMessage(operation, new ArraySegment<byte>(body, 0, firstPacketLength - 16).ToArray());
                    if (packetData.Length > firstPacketLength)
                    {
                        packetData = new ArraySegment<byte>(packetData, firstPacketLength, packetData.Length - firstPacketLength).ToArray();
                        continue;
                    }

                    break;
                case 3:
                    packetData = Brotli.DecompressBuffer(body, 0, body.Length);
                    continue;
                default:
                    throw new UnknownVersionException(version);
            }

            break;
        }
    }

    private async Task ReceiveMessage()
    {
        var buffer = new List<byte>();
        while (_clientWebSocket.State == WebSocketState.Open)
        {
            var tempBuffer = new byte[1024];
            var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(tempBuffer), _webSocketCancelToken.Token);
            buffer.AddRange(new ArraySegment<byte>(tempBuffer, 0, result.Count));
            if (!result.EndOfMessage) continue;
            try
            {
                DecodePacket(buffer.ToArray());
            }
            catch (Exception e)
            {
                OnDecodeError(e.Message, e);
            }
            finally
            {
                buffer.Clear();
            }
        }

        throw new OperationCanceledException();
    }

    private async Task SendHeartbeat(ArraySegment<byte> heartPacket)
    {
        while (_clientWebSocket.State == WebSocketState.Open)
        {
            await _clientWebSocket.SendAsync(heartPacket, WebSocketMessageType.Binary, true, _webSocketCancelToken.Token);
            await Task.Delay(TimeSpan.FromSeconds(20), _webSocketCancelToken.Token);
        }

        throw new OperationCanceledException();
    }

    private static byte[] ToBigEndianBytes(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }

    private static byte[] ToBigEndianBytes(short value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }

    private static ArraySegment<byte> CreateWsPacket(ClientOperation operation, byte[] body)
    {
        var packetLength = 16 + body.Length;
        var result = new byte[packetLength];
        Buffer.BlockCopy(ToBigEndianBytes(packetLength), 0, result, 0, 4);
        Buffer.BlockCopy(ToBigEndianBytes((short)16), 0, result, 4, 2);
        Buffer.BlockCopy(ToBigEndianBytes((short)1), 0, result, 6, 2);
        Buffer.BlockCopy(ToBigEndianBytes((int)operation), 0, result, 8, 4);
        Buffer.BlockCopy(ToBigEndianBytes(1), 0, result, 12, 4);
        Buffer.BlockCopy(body, 0, result, 16, body.Length);
        return new ArraySegment<byte>(result);
    }

    private static (ulong?, ulong?) GetRoomIdAndUid(ulong shortRoomId)
    {
        try
        {
            var url = $"https://api.live.bilibili.com/xlive/web-room/v1/index/getRoomBaseInfo?room_ids={shortRoomId}&req_biz=web/";
            var result = new HttpClient().GetStringAsync(url).Result;
            var jsonResult = (JObject)JsonConvert.DeserializeObject(result);
            var roomInfo = (JObject)jsonResult?["data"]?["by_room_ids"]?.Values().FirstOrDefault();
            var roomId = (ulong?)roomInfo?.GetValue("room_id");
            var uid = (ulong?)roomInfo?.GetValue("uid");
            if (roomId is null || uid is null) throw new InvalidRoomIdException();
            return (roomId, uid);
        }
        catch (InvalidRoomIdException)
        {
            throw;
        }
        catch
        {
            throw new NetworkException();
        }
    }

    /// <summary>
    ///     关闭当前对象中的WebSocket
    /// </summary>
    public async Task Close()
    {
        _webSocketCancelToken?.Cancel();
        if (_clientWebSocket is not null && _clientWebSocket.State == WebSocketState.Open)
            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
    }

    /// <summary>
    ///     连接指定的直播间
    /// </summary>
    /// <param name="roomId">直播间id,可以是短位id</param>
    public async Task Connect(ulong roomId)
    {
        if (_webSocketCancelToken is not null) throw new ConnectAlreadyRunningException();
        try
        {
            _webSocketCancelToken = new CancellationTokenSource();
            (_roomId, _uid) = GetRoomIdAndUid(roomId);
            _clientWebSocket = new ClientWebSocket();
            var authBody = new { uid = _uid, roomid = _roomId, protover = 3, platform = "web", type = 2 };
            var authPacket = CreateWsPacket(ClientOperation.OpAuth, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(authBody)));
            var heartPacket = CreateWsPacket(ClientOperation.OpHeartbeat, Array.Empty<byte>());
            await _clientWebSocket.ConnectAsync(new Uri(WsHost), _webSocketCancelToken.Token);
            await _clientWebSocket.SendAsync(authPacket, WebSocketMessageType.Binary, true, _webSocketCancelToken.Token);
            await Task.WhenAll(ReceiveMessage(), SendHeartbeat(heartPacket));
        }
        catch (OperationCanceledException)
        {
            OnWebSocketClose("WebSocket主动关闭", 0);
            throw new WebSocketCloseException();
        }
        catch (WebSocketException)
        {
            OnWebSocketError("WebSocket异常关闭", -1);
            _webSocketCancelToken?.Cancel();
            throw new WebSocketErrorException();
        }
        finally
        {
            _roomId = null;
            _uid = null;
            _clientWebSocket = null;
            _webSocketCancelToken = null;
        }
    }

    private enum ClientOperation
    {
        OpHeartbeat = 2,
        OpAuth = 7
    }

    private enum ServerOperation
    {
        OpHeartbeatReply = 3,
        OpSendSmsReply = 5,
        OpAuthReply = 8
    }
}