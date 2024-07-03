using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace Gebhardt.Acc.Common.TelegramCommunication.Communication;

/// <summary>
/// Provides a server for socket communication
/// </summary>
public class Server : SocketBase
{
    private TelegramCommunicationConfiguration Configuration { get; }

    private Socket? _serverSocket;

    /// <inheritdoc cref="Client"/>>
    public Server(ILogger<SocketBase> logger, TelegramCommunicationConfiguration configuration) : base(logger)
    {
        Configuration = configuration;
    }
    
    /// <inheritdoc cref="SocketBase"/>>
    public override bool Initialize()
    {
        try{
            if (_serverSocket == null)
            {
                CreateSocket();
            }

            WaitForClient();
            SetTcpKeepAlive(Configuration.UseTcpKeepAlive, Configuration.TcpKeepAliveInterval, 1000, Configuration.TcpKeepAliveRetries);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not connect, got : {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc cref="SocketBase"/>>
    public override void Disconnect()
    {
        base.Disconnect();
        DisconnectServer();
    }
    
    private void CreateSocket()
    {
        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
        _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        var ipAddress = IPAddress.Parse(Configuration.IpAddress);
        var ipEndPoint = new IPEndPoint(ipAddress, Configuration.Port);
        _serverSocket.Bind(ipEndPoint);

        //Allow only one client
        _serverSocket.Listen(1);
    }

    private void WaitForClient()
    {
        Logger.LogInformation("Waiting for client to connect");
        Socket = _serverSocket!.Accept();
        Logger.LogInformation("Client connected");
        Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
    }

    private void DisconnectServer()
    {
        try
        {
            if (_serverSocket == null)
            {
                return;
            }
            _serverSocket.Shutdown(SocketShutdown.Both);
            _serverSocket.Close();
            _serverSocket = null;
        }
        catch (SocketException socketException)
        {
            Logger.LogError("Could not disconnect, got : {ErrorCode} / {ErrorMessage}", socketException.ErrorCode, socketException.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not disconnect, got : {ErrorMessage}", ex.Message);
        }
    }
}