using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.TelegramCommunication.Configuration;

/// <summary>
/// Provides a configuration for the telegram communication
/// </summary>
[PublicAPI]
public class TelegramCommunicationConfiguration
{
    /// <inheritdoc cref="TelegramCommunicationConfiguration"/>>
    public TelegramCommunicationConfiguration(string ipAddress, int port, int tcpKeepAliveInterval, bool useTcpKeepAlive, int tcpKeepAliveRetryCount, int waitingTime, int lifeTelegramInterval)
    {
        IpAddress = ipAddress;
        Port = port;
        TcpKeepAliveInterval = tcpKeepAliveInterval;
        TcpKeepAliveRetries = tcpKeepAliveRetryCount;
        UseTcpKeepAlive = useTcpKeepAlive;
        TelegramWaitingTime = waitingTime;
        LifeTelegramInterval = lifeTelegramInterval;
    }

    /// <inheritdoc cref="TelegramCommunicationConfiguration"/>>
    public TelegramCommunicationConfiguration()
    {
        IpAddress = string.Empty;
    }

    /// <summary>
    /// Server or remote ip address
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Interval for tcp keep alive [ms]
    /// </summary>
    public int TcpKeepAliveInterval { get; set; }

    /// <summary>
    /// Retry count for tcp keep alive
    /// </summary>
    public int TcpKeepAliveRetries { get; set; }

    /// <summary>
    /// Use tcp keep alive
    /// </summary>
    public bool UseTcpKeepAlive { get; set; }

    /// <summary>
    ///The interval at which life telegrams are sent [s]
    /// </summary>
    public int LifeTelegramInterval { get; set; }

    /// <summary>
    /// Waiting time for telegrams [ms]
    /// </summary>
    public int TelegramWaitingTime { get; set; }
}