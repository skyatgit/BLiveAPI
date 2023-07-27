using System;

namespace BLiveAPI;

/// <summary>
///     同一对象的Connect方法重复运行的异常
/// </summary>
public class ConnectAlreadyRunningException : Exception
{
    /// <inheritdoc cref="ConnectAlreadyRunningException" />
    public ConnectAlreadyRunningException() : base("该对象的Connect方法已经在运行中,禁止重复运行")
    {
    }
}

/// <summary>
///     房间号无效的异常
/// </summary>
public class InvalidRoomIdException : Exception
{
    /// <inheritdoc cref="InvalidRoomIdException" />
    public InvalidRoomIdException() : base("无效的房间号")
    {
    }
}

/// <summary>
///     未知的ServerOperation异常
/// </summary>
public class UnknownServerOperationException : Exception
{
    /// <inheritdoc cref="UnknownServerOperationException" />
    public UnknownServerOperationException(object value) : base($"未知的ServerOperation:{value}")
    {
    }
}

/// <summary>
///     未知的Version异常
/// </summary>
public class UnknownVersionException : Exception
{
    /// <inheritdoc cref="UnknownVersionException" />
    public UnknownVersionException(object value) : base($"未知的Version:{value}")
    {
    }
}

/// <summary>
///     网络异常
/// </summary>
public class NetworkException : Exception
{
    /// <inheritdoc cref="NetworkException" />
    public NetworkException() : base("网络异常")
    {
    }
}

/// <summary>
///     字节集长度错误
/// </summary>
public class InvalidBytesLengthException : Exception
{
    /// <inheritdoc cref="InvalidBytesLengthException" />
    public InvalidBytesLengthException() : base("字节集长度错误")
    {
    }
}