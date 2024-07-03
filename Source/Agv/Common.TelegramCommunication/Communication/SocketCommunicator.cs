using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.TelegramCommunication.Communication;

/// <summary>
/// Provides an abstract communicator for sender and receiver implementations
/// using socket communication
/// </summary>
/// <typeparam name="TTelegramType"></typeparam>
public abstract class SocketCommunicator<TTelegramType>
    where TTelegramType : TelegramBase
{
    private readonly object _runLock = new();
    
    private volatile bool _run;

    private readonly object _restartMeLock = new();

    /// <summary>
    /// Thread in which the communication is executed.
    /// </summary>
    private Thread _thread = null!;

    /// <inheritdoc cref="SocketCommunicator{TTelegramType}"/>>
    protected SocketCommunicator(SocketBase socket, string name, TelegramProcessorBase<TTelegramType> telegramProcessor, ILogger<SocketCommunicator<TTelegramType>> logger)
    {
        Socket = socket;
        TelegramProcessor = telegramProcessor;
        Logger = logger;
        Name = name;
    }

    /// <summary>
    /// The name of the thread
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// 
    /// </summary>
    public bool SetRun
    {
        set
        {
            lock (_runLock)
            {
                _run = value;
            }
        }
    }

    /// <summary>
    /// True, if the communication shall be restarted
    /// </summary>
    private bool _restartMe;

    /// <summary>
    /// True, if the communication shall be restarted
    /// </summary>
    public virtual bool RestartMe
    {
        get
        {
            lock (_restartMeLock)
            {
                return _restartMe;
            }
        }

        protected set
        {
            lock (_restartMeLock)
            {
                _restartMe = value;
            }
        }
    }

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger<SocketCommunicator<TTelegramType>> Logger;

    /// <summary>
    /// The restart request
    /// </summary>
    public event EventHandler? RestartRequest;

    /// <summary>
    /// The socket connection (<see cref="Server"/>> or <see cref="Client"/>)
    /// </summary>
    protected SocketBase Socket { get; }

    /// <summary>
    ///  The processor for this telegram type.
    /// </summary>
    protected TelegramProcessorBase<TTelegramType> TelegramProcessor { get; }

    /// <summary>
    /// Called to trigger the restart event
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    [PublicAPI]
    protected void SendRestartRequest(string errorMessage)
    {
        Logger.LogError(errorMessage);
        SendRestartRequest();
    }

    /// <summary>
    /// Called to trigger the restart event
    /// </summary>
    /// <param name="exception">Exception</param>
    [PublicAPI]
    protected void SendRestartRequest(Exception exception)
    {
        Logger.LogError(exception.Message);
        SendRestartRequest();
    }

    private void SendRestartRequest()
    {
        var restartRequest = RestartRequest;
        restartRequest?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Stop communicating
    /// </summary>
    protected virtual void Stop(bool reset = false)
    {
        Socket.Disconnect();
    }

    /// <summary>
    /// Start the communication
    /// </summary>
    /// <returns>True if starting the communication was successful</returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public bool Start()
    {
        try
        {
            SetRun = true;
            _thread = new Thread(Communicate);
            _thread.Start();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not start the communication, got : {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Initiates stopping of the thread
    /// </summary>
    public virtual void BeginStop()
    {
        Logger.LogDebug("Thread {Name} should stop", Name);
        SetRun = false;
    }

    /// <summary>
    /// Stops the thread
    /// </summary>
    public void FinishStop(bool reset = false)
    {
        try
        {
            if (_thread != null! && _thread.IsAlive)
            {
                if (!_thread.Join(60000))
                {
                    Logger.LogWarning("Thread {Name} did not stop, aborting", Name);
                    var cancellationToken = new CancellationToken(canceled:true);
                    cancellationToken.ThrowIfCancellationRequested();
                    _thread = null!;
                }
            }
            Stop(reset);
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not finish stopping, got : {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Restart the communicator
    /// </summary>
    /// <returns></returns>
    public void Restart()
    {
        RestartMe = false;
        BeginStop();
        FinishStop();
        Start();
    }

    /// <summary>
    /// Exchange messages
    /// </summary>
    protected abstract bool ExchangeMessages();

    private void Communicate()
    {
        Thread.CurrentThread.Name ??= Name;

        Logger.LogDebug("Started thread for {Name}", Name);
        while (_run)
        {
            try
            {
                Logger.LogDebug("Call exchange messages");
                if (ExchangeMessages())
                {
                    Logger.LogDebug("Exchange was successful");
                }
            }
            catch (Exception ex)
            {
                if (_run)
                {
                    Logger.LogError("Could not communicate, got : {ErrorMessage}", ex.Message);
                    SetRun = false;
                    RestartMe = true;
                }
            }
        }
    }
}