using System;

namespace BLiveAPI;

public class ConnectAlreadyRunningException : Exception
{
    public ConnectAlreadyRunningException() : base("该对象的Connect方法已经在运行中,禁止重复运行")
    {
    }
}

public class InvalidRoomIdException : Exception
{
    public InvalidRoomIdException() : base("无效的房间号")
    {
    }
}

public class UnknownServerOperationException : Exception
{
    public UnknownServerOperationException(object value) : base($"未知的ServerOperation:{value}")
    {
    }
}

public class UnknownVersionException : Exception
{
    public UnknownVersionException(object value) : base($"未知的Version:{value}")
    {
    }
}

public class NetworkException : Exception
{
    public NetworkException() : base("网络异常")
    {
    }
}

public class InvalidBytesLengthException : Exception
{
    public InvalidBytesLengthException() : base("字节集长度错误")
    {
    }
}