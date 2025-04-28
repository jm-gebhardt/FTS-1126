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

    private CancellationTokenSource? _cts;

    private const int ThreadWaitingTime = 10000;

    private readonly TimeSpan _threadSleepingTime = TimeSpan.FromMilliseconds(100);

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
            _cts =  new CancellationTokenSource();  
            _thread = new Thread( _ => Communicate(_cts.Token));
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
        Logger.LogInformation("Thread {Name} should stop", Name);
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
                _cts!.Cancel();
                Logger.LogDebug("Stopping by cancellation of token {CancellationRequested}, waiting {WaitingTime}ms for thread to finish", _cts.Token.IsCancellationRequested, ThreadWaitingTime);
                if (!_thread.Join(ThreadWaitingTime))
                {
                    Logger.LogWarning("Thread {Name} did not stop, aborting", Name);
                    _thread = null!;
                }
            }
            Stop(reset);
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not finish stopping, got : {ErrorMessage}", ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
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
        bool connected = Socket.IsConnected();
        while (!connected)
        {
            Task.Delay(ThreadWaitingTime);
            Logger.LogDebug("Trying to connect to socket");
            connected = Socket.Initialize();
        }
        Start();
    }

    /// <summary>
    /// Restart the communicator
    /// </summary>
    public async Task RestartAsync()
    {
        RestartMe = false;
        BeginStop();
        FinishStop();
        bool connected = Socket.IsConnected();
        while (!connected)
        {
            await Task.Delay(ThreadWaitingTime);
            Logger.LogDebug("Trying to connect to socket");
            connected = await Socket.InitializeAsync();
        }
        Start();
    }

    private void RestartWorker()
    {
        SetRun = true;
        Start();
    }

    /// <summary>
    /// Exchange messages
    /// </summary>
    protected abstract bool ExchangeMessages();

    private void Communicate(CancellationToken cancellationToken)
    {
        try
        {
            Thread.CurrentThread.Name ??= Name;

            Logger.LogDebug("Started thread for {Name}, cancellation requested {IsCancellationRequested}", Name, cancellationToken.IsCancellationRequested);
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not communicate, got : {ErrorMessage} - restarting worker", ex.Message);
            RestartWorker();
        }

        while (_run && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (ExchangeMessages())
                {
                    Logger.LogDebug("Exchange was successful");
                }
                Thread.Sleep(_threadSleepingTime);
            }
            catch (Exception ex)
            {
                if (_run)
                {
                    Logger.LogError("Could not communicate, got : {ErrorMessage}", ex.Message);
                    SetRun = false;
                    RestartMe = true;
                    SendRestartRequest("Failed to communicate");
                }
            }
        }
        Logger.LogDebug("Stopped communicating in thread for {Name}", Name);
        if (!cancellationToken.IsCancellationRequested)
        {
            Logger.LogDebug("Restarting worker");
            RestartWorker();
        }
    }
}