using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

namespace Gebhardt.Acc.Common.TelegramCommunication.Communication;

/// <summary>
/// Provides a base class for socket communication
/// </summary>
public abstract class SocketBase
{
    private const int SocketTimeOut = 10060;

    /// <summary>
    /// The socket
    /// </summary>
    protected Socket? Socket;

    /// <summary>
    /// The telegram send on receive timeout.
    /// </summary>
    public static string TimeoutTelegram => "timeout";

    /// <summary>
    /// True, if the socket is connected.
    /// </summary>
    public abstract bool IsConnected();

    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger<SocketBase> Logger;

    /// <inheritdoc cref="SocketBase"/>>
    protected SocketBase(ILogger<SocketBase> logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Initialize
    /// </summary>
    public abstract bool Initialize();

    /// <summary>
    /// Initialize async
    /// </summary>
    public abstract Task<bool> InitializeAsync();

    /// <summary>
    /// Disconnect
    /// </summary>
    public virtual void Disconnect()
    {
        try
        {
            if (Socket == null)
            {
                return;
            }
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
            Socket = null;
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

    /// <summary>
    /// Receive a telegram
    /// </summary>
    /// <param name="telegram">The received telegram</param>
    /// <param name="telegramLength">The expected telegram length</param>
    /// <param name="receiveTimeout">Maximum timeout [ms]</param>
    /// <param name="encoding">Encoding to bytes</param>
    public bool TryReceive(out string telegram, int telegramLength, int receiveTimeout, Encoding encoding)
    {
        if (telegramLength == 0)
        {
            return TryReceiveWithUnknownLength(out telegram, receiveTimeout, encoding);
        }
        int receivedBytesCount;

        if (Socket == null)
        {
            Initialize();
        }

        byte[] receiveBuffer = new byte[telegramLength];
        try
        {
            Socket!.ReceiveTimeout = receiveTimeout;

            receivedBytesCount = Socket.Receive(receiveBuffer);
        }
        catch (SocketException socketException)
        {
            if (socketException.ErrorCode == SocketTimeOut)
            {
                telegram = TimeoutTelegram;
                return false;
            }
            Logger.LogError("Could not receive telegram, got : {ErrorCode} / {ErrorMessage}", socketException.ErrorCode, socketException.Message);
            telegram = null!;
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not receive telegram, got {ErrorMessage}", ex.Message);
            telegram = null!;
            return false;
        }

        if (receivedBytesCount == 0)
        {
            Logger.LogError("Received empty telegram");
            telegram = null!;
            return false;
        }

        try
        {
            byte[] receiveBufferTrimmed = new byte[receivedBytesCount];
            Array.Copy(receiveBuffer, 0, receiveBufferTrimmed, 0, receivedBytesCount);

            telegram = encoding.GetString(receiveBufferTrimmed);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not decode telegram, got {ErrorMessage}", ex.Message);
            telegram = null!;
            return false;
        }
    }

    /// <summary>
    /// Receive a telegram with unknown length.
    /// </summary>
    /// <param name="telegram">The received telegram</param>
    /// <param name="receiveTimeout">Maximum timeout [ms]</param>
    /// <param name="encoding">Encoding to bytes</param>
    private bool TryReceiveWithUnknownLength(out string telegram, int receiveTimeout, Encoding encoding)
    {
       const int bufferSize = 256;
        byte[] buffer = new byte[bufferSize];
        var message = new StringBuilder();

        try
        {
            if (Socket == null)
            {
                Initialize();
            }
            Socket!.ReceiveTimeout = receiveTimeout;
            while (true)
            {
                int bytesRead = Socket!.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                if (bytesRead > 0)
                {
                    message.Append(encoding.GetString(buffer, 0, bytesRead));
                }
                break;
            }
        }
        catch (SocketException socketException)
        {
            if (socketException.ErrorCode == SocketTimeOut)
            {
                telegram = TimeoutTelegram;
                return false;
            }

            Logger.LogError("Could not receive telegram, got : {ErrorCode} / {ErrorMessage}", socketException.ErrorCode, socketException.Message);
            telegram = string.Empty;
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not receive telegram, got {ErrorMessage}", ex.Message);
            telegram = null!;
            return false;
        }

        if (message.Length == 0)
        {
            Logger.LogError("Received empty telegram");
            telegram = null!;
            return false;
        }

        telegram = message.ToString();
        return true;
    }

    /// <summary>
    /// Try to send a telegram
    /// </summary>
    /// <param name="telegram">The telegram.</param>
    /// <param name="encoding">Encoding of the telegram</param>
    public bool TrySend(string telegram, Encoding encoding)
    {
        if (Socket == null)
        {
            Initialize();
        }
        try
        {
            byte[] sendBuffer = encoding.GetBytes(telegram);

            Socket!.Send(sendBuffer);
        }
        catch (SocketException socketException)
        {
            Logger.LogError("Could not receive telegram, got : {ErrorCode} / {ErrorMessage}", socketException.ErrorCode, socketException.Message);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not send telegram, got {ErrorMessage}", ex.Message);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Activate / deactivate tcp keep alive packet sending
    /// </summary>
    /// <param name="on">True to switch the keep alive on</param>
    /// <param name="time">Time [ms] after which a keep alive packet will be sent.</param>
    /// <param name="resendInterval">Interval after which the packet is resend, if there was no response.</param>
    /// <param name="retryCount"></param>
    protected void SetTcpKeepAlive(bool on, int time, int resendInterval, int retryCount=5)
    {
        Socket!.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, resendInterval);
        Socket!.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, time);
        Socket!.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, retryCount);
        Socket!.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, on);
    }
}