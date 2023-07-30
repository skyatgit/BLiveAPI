# BLiveAPI

B站直播间弹幕野生接口。

```c#
//创建一个BLiveApi对象
var api = new BLiveApi();
//用于接收认证消息的方法(没啥用,可以不写)
private void OpAuthReplyEvent(object sender, (JObject authReply, byte[] rawData) e)
{
}
//用于接收心跳消息的方法
private void OpHeartbeatReplyEvent(object sender, (int heartbeatReply, byte[] rawData) e)
{
}
//用于接收主动关闭消息的方法(主动调用api.Close()),同时会触发异常
private void WebSocketCloseEvent(object sender, (string message, int code) e)
{
}
//用于接收被动关闭消息的方法(一般是网络错误等原因),同时会触发异常
private void WebSocketErrorEvent(object sender, (string message, int code) e)
{
}
//用于接收API内部解码错误,一般情况下不会触发,触发B站改逻辑或其他特殊情况,此消息触发时不会引起异常
private void DecodeErrorEvent(object sender, (string message, Exception e) e)
{
}
//用于接收API内部提供的一个简单处理过后的弹幕消息的方法
//此方法订阅DanmuMsg事件时会和使用者创建并绑定了携带[TargetCmd("DANMU_MSG")]的方法一样屏蔽掉携带OTHERS的方法(可看下面的示例)
private void DanmuMsgEvent(object sender, (string msg, long userId, string userName, string face, JObject rawData) e)
{
}
//当方法与OpSendSmsReply绑定时需要使用[TargetCmd("cmd1","cmd2"...)]设置方法想要接收的命令,建议每个方法只设置1个命令
//此方法是使用者自定义的用于接收OpSendSmsReply事件中SEND_GIFT命令对应的事件的方法
[TargetCmd("SEND_GIFT")]
private void OnSendGiftEvent(object sender, (string cmd, string hitCmd, JObject rawData) e)
{
}
//TargetCmd支持填入ALL和OTHERS
//当携带ALL或者没有标注[TargetCmd("cmd1","cmd2"...)]时,该方法会无差别的接收所有SMS消息,但会首先命中TargetCmd参数列表中的其他命令
//当cmd只命中ALL或此方法未携带[TargetCmd("cmd1","cmd2"...)]时,不视作命令被命中,携带OTHERS的方法仍然会被Invoke
[TargetCmd("ALL")]
private void OnAllEvent(object sender, (string cmd, string hitCmd, JObject rawData) e)
{
}
//当携带OTHERS时,该方法会接收未被其他方法命中的SMS消息,但TargetCmd参数列表中的其他命令被命中时不会被再次Invoke
[TargetCmd("OTHERS")]
private  void OtherMessagesEvent(object sender, (string cmd, string hitCmd, JObject rawData) e)
{
}

//绑定事件
api.OpAuthReply += OpAuthReplyEvent;
api.OpHeartbeatReply += OpHeartbeatReplyEvent;
api.WebSocketClose += WebSocketCloseEvent;
api.WebSocketError += WebSocketErrorEvent;
api.DecodeError += DecodeErrorEvent;
api.DanmuMsg += DanmuMsgEvent;
api.OpSendSmsReply += OnSendGiftEvent;
api.OpSendSmsReply += OnAllEvent;
api.OpSendSmsReply += OtherMessagesEvent;

//连接到某个直播间,Connect内有可能会抛出一些回事WebSocket断开连接的异常,需要监听并处理
try
{
    await api.Connect(1234);
}
catch (Exception e)
{
    Console.WriteLine(e);
}
//可以通过Close方法主动关闭WebSocket连接
api.Close();
```

