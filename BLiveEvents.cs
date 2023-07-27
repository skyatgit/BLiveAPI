﻿using System;
using Newtonsoft.Json.Linq;

namespace BLiveAPI;

public abstract class BLiveEvents
{
    public delegate void BLiveEventHandler<in TEventArgs>(object sender, TEventArgs e);

    /// <summary>
    ///     服务器回复的认证消息
    /// </summary>
    public event BLiveEventHandler<(JObject authReply, byte[] rawData)> OpAuthReply;

    protected void OnOpAuthReply(JObject authReply, byte[] rawData)
    {
        OpAuthReply?.Invoke(this, (authReply, rawData));
    }

    /// <summary>
    ///     服务器回复的心跳消息
    /// </summary>
    public event BLiveEventHandler<(int heartbeatReply, byte[] rawData)> OpHeartbeatReply;

    protected void OnOpHeartbeatReply(int heartbeatReply, byte[] rawData)
    {
        OpHeartbeatReply?.Invoke(this, (heartbeatReply, rawData));
    }

    /// <summary>
    ///     服务器发送的SMS消息
    /// </summary>
    public event BLiveEventHandler<(JObject sendSmsReply, byte[] rawData)> OpSendSmsReply;

    protected void OnOpSendSmsReply(JObject sendSmsReply, byte[] rawData)
    {
        OpSendSmsReply?.Invoke(this, (sendSmsReply, rawData));
    }

    /// <summary>
    ///     弹幕消息
    /// </summary>
    public event BLiveEventHandler<(string msg, long userId, string userName, string face, JObject rawData)> DanmuMsg;

    protected void OnDanmuMsg(string msg, long userId, string userName, string face, JObject rawData)
    {
        DanmuMsg?.Invoke(this, (msg, userId, userName, face, rawData));
    }

    /// <summary>
    ///     其他未处理的消息
    /// </summary>
    public event BLiveEventHandler<(string cmd, JObject rawData)> OtherMessages;

    protected void OnOtherMessages(string cmd, JObject rawData)
    {
        OtherMessages?.Invoke(this, (cmd, rawData));
    }

    /// <summary>
    ///     WebSocket异常关闭
    /// </summary>
    public event BLiveEventHandler<(string message, int code)> WebSocketError;

    protected void OnWebSocketError(string message, int code)
    {
        WebSocketError?.Invoke(this, (message, code));
    }

    /// <summary>
    ///     WebSocket主动关闭
    /// </summary>
    public event BLiveEventHandler<(string message, int code)> WebSocketClose;

    protected void OnWebSocketClose(string message, int code)
    {
        WebSocketClose?.Invoke(this, (message, code));
    }

    /// <summary>
    ///     解析消息过程出现的错误，不影响WebSocket正常运行，所以不抛出异常
    /// </summary>
    public event BLiveEventHandler<(string message, Exception e)> DecodeError;

    protected void OnDecodeError(string message, Exception e)
    {
        DecodeError?.Invoke(this, (message, e));
    }
}