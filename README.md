# BLiveAPI

B站直播间弹幕野生接口。

```c#
//定义一些方法供api事件使用
private void OpAuthReplyEvent(object sender, (JObject authReply, byte[] rawData) e)
{
}
private void OpHeartbeatReplyEvent(object sender, (int heartbeatReply, byte[] rawData) e)
{
}
private void DanmuMsgEvent(object sender, (string msg, long userId, string userName, string face, JObject rawData) e)
{
}
private void WebSocketCloseEvent(object sender, (string message, int code) e)
{
}
private void WebSocketErrorEvent(object sender, (string message, int code) e)
{
}
//创建一个BLiveApi对象
var api = new BLiveApi();
//绑定事件
api.OpAuthReply += OpAuthReplyEvent;
api.OpHeartbeatReply += OpHeartbeatReplyEvent;
api.DanmuMsg += DanmuMsgEvent;
api.WebSocketClose += WebSocketCloseEvent;
api.WebSocketError += WebSocketErrorEvent;
api.DecodeError += DecodeErrorEvent;
//连接到某个直播间,Connect内有可能会抛出一些回事WebSocket断开连接的异常,需要监听并处理
try
{
    await _api.Connect(1234);
}
catch (Exception e)
{
    Console.WriteLine(e);
}
//可以通过Close方法主动关闭WebSocket连接
api.Close();
```

