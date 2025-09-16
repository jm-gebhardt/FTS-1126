using Microsoft.Extensions.Logging;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using System.Net.Sockets;

namespace Gebhardt.Acc.Common.TelegramCommunication.Communication;

/// <summary>
/// Provides a client for socket communication
/// </summary>
public class Client : SocketBase
{
    private const int TcpResendInterval = 1000;

    private TelegramCommunicationConfiguration Configuration { get; }

    /// <inheritdoc cref="Client"/>>
    public Client(ILogger<Client> logger, TelegramCommunicationConfiguration configuration) : base(logger)
    {
        Configuration = configuration;
    }

    /// <inheritdoc cref="SocketBase"/>>
    public override bool TryInitialize()
    {
        try
        {
            Disconnect();
            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            if (OperatingSystem.IsWindows())
            {
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            }
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            Logger.LogInformation("Connecting to {Server}:{Port}", Configuration.IpAddress, Configuration.Port);
            Socket.Connect(Configuration.IpAddress, Configuration.Port);
            SetTcpKeepAlive(Configuration.UseTcpKeepAlive, Configuration.TcpKeepAliveInterval, TcpResendInterval, Configuration.TcpKeepAliveRetries);

            Logger.LogInformation("Connected to {Server}:{Port}", Configuration.IpAddress, Configuration.Port);
            return true;
        }
        catch (SocketException socketException)
        {
            Logger.LogError("Could not connect, got : {ErrorCode} / {ErrorMessage}", socketException.ErrorCode, socketException.Message);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not connect, got : {ErrorMessage} {Trace}", ex.Message, ex.StackTrace);
            return false;
        }
    }

    /// <inheritdoc cref="SocketBase"/>>
    public override Task<bool> TryInitializeAsync()
    {
        return Task.FromResult(TryInitialize());
    }
}