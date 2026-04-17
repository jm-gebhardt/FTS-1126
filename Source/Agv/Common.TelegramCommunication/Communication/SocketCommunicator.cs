using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
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
    private const int ConnectionWaitingTime = 10000;
    private const int MaxReconnectAttempts = 100;

    private bool _isRunning;
    private bool _shouldReconnectSocket;
    private CancellationTokenSource? _cts;
    private Task? _communicationTask;
    private readonly TimeSpan _taskSleepingTime = TimeSpan.FromMilliseconds(100);
    private readonly SemaphoreSlim _startSemaphore = new(1, 1);
    private readonly SemaphoreSlim _reconnectSemaphore = new(1, 1);

    /// <summary>
    /// True, if the socket is currently connected. Setting this property fires
    /// <see cref="ConnectionStateChanged"/> invoked when the value actually changes.
    /// </summary>
    private bool IsConnected
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            ConnectionStateChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// Invoked when the socket connection state changes.
    /// The bool parameter indicates whether the socket is now connected.
    /// Only fires on actual state transitions.
    /// </summary>
    public Action<bool>? ConnectionStateChanged { get; set; }

    /// <summary>
    /// The name of the communication task
    /// </summary>
    protected string Name { get; }

    /// <summary>
    /// The logger
    /// </summary>
    protected ILogger<SocketCommunicator<TTelegramType>> Logger { get; }

    /// <summary>
    /// The socket connection (<see cref="Server"/> or <see cref="Client"/>)
    /// </summary>
    protected SocketBase Socket { get; }

    /// <summary>
    /// The one who handles the telegrams
    /// </summary>
    protected TelegramProcessorBase<TTelegramType> TelegramProcessor { get; }

    /// <inheritdoc cref="SocketCommunicator{TTelegramType}"/>>
    protected SocketCommunicator(SocketBase socket, string name, TelegramProcessorBase<TTelegramType> telegramProcessor, ILogger<SocketCommunicator<TTelegramType>> logger)
    {
        Socket = socket;
        TelegramProcessor = telegramProcessor;
        Logger = logger;
        Name = name;
    }

    private void DisconnectSocket() => Socket.Disconnect();

    /// <summary>
    /// Exchange messages
    /// </summary>
    protected abstract bool ExchangeMessages();

    private void RequestSocketReconnect(Exception exception)
    {
        Logger.LogError("Socket reconnection requested due to exception: {ErrorMessage}", exception.Message);
        RequestSocketReconnect($"Failed to communicate: {exception.Message}");
    }

    /// <summary>
    /// Call to request a socket reconnection
    /// </summary>
    /// <param name="reason">Error message describing the reason for reconnection</param>
    protected void RequestSocketReconnect(string reason)
    {
        Logger.LogInformation("Socket reconnection requested due to: {Reason}", reason);
        _shouldReconnectSocket = true;
    }

    /// <summary>
    /// Start the communication
    /// </summary>
    /// <returns>True if starting the communication was successful</returns>
    public void Start()
    {
        if (!_startSemaphore.Wait(0))
        {
            Logger.LogDebug("Start called while already starting/running, skipping for {Name}", Name);
            return;
        }

        try
        {
            if (_isRunning && _communicationTask is { IsCompleted: false })
            {
                Logger.LogInformation("Communication for {Name} is already running", Name);
                return;
            }

            TelegramProcessor.Init();
            _isRunning = true;
            _cts = new CancellationTokenSource();
            _communicationTask = CommunicateAsync(_cts.Token);
        }
        finally
        {
            _startSemaphore.Release();
        }
    }

    /// <summary>
    /// Stops the communication task and disconnects the socket
    /// </summary>
    public async Task StopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping task {Name}", Name);
            _isRunning = false;

            if (_communicationTask is { IsCompleted: false })
            {
                if (_cts != null)
                {
                    await _cts.CancelAsync();
                    Logger.BeginScope("Stopping by cancellation of token {CancellationRequested}", _cts.Token.IsCancellationRequested);
                }

                // Wait for task to complete within timeout
                var completedTask = await Task.WhenAny(_communicationTask, Task.Delay(ConnectionWaitingTime));

                if (completedTask != _communicationTask)
                {
                    Logger.LogWarning("Task {Name} did not stop within timeout", Name);
                }
            }

            DisconnectSocket();
            IsConnected = false;
            TelegramProcessor.ResetCounter();
        }
        catch (Exception ex)
        {
            Logger.LogError("Could not stop the communication, got : {ErrorMessage}", ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _communicationTask = null;
        }
    }

    private async Task ReconnectSocketAsync()
    {
        if (!await _reconnectSemaphore.WaitAsync(0))
        {
            Logger.LogWarning("Already reconnecting socket for {Name}", Name);
            return;
        }

        try
        {
            Logger.LogInformation("Initiating socket reconnection for {Name}", Name);
            _shouldReconnectSocket = false;

            DisconnectSocket();

            IsConnected = false;
            int attempts = 0;

            while (!IsConnected && attempts < MaxReconnectAttempts)
            {
                attempts++;
                Logger.LogInformation("Trying to connect socket for {Name} (attempt {Attempt}/{MaxAttempts})", Name, attempts, MaxReconnectAttempts);
                IsConnected = await Socket.TryInitializeAsync();

                if (!IsConnected && attempts < MaxReconnectAttempts)
                {
                    await Task.Delay(ConnectionWaitingTime);
                }
            }

            if (IsConnected)
            {
                Logger.LogInformation("Successfully connected socket for {Name}", Name);
            }
            else
            {
                Logger.LogError("Failed to connect socket for {Name} after {MaxAttempts} attempts", Name, MaxReconnectAttempts);

                if (_isRunning)
                {
                    // if task is still supposed to be running, but we failed to connect, throw.
                    throw new Exception($"Failed to connect socket for {Name} after {MaxReconnectAttempts} attempts");
                }
            }
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    private async Task CommunicateAsync(CancellationToken cancellationToken)
    {
        Thread.CurrentThread.Name ??= Name;
        Logger.LogInformation("Started communication-task for {Name}", Name);

        IsConnected = true;

        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                try
                {
                    if (ExchangeMessages())
                    {
                        Logger.LogDebug("Exchange was successful");
                    }

                    if (TelegramProcessor.ShouldRestart())
                    {
                        RequestSocketReconnect("TelegramProcessor requested reconnection");
                    }

                    await Task.Delay(_taskSleepingTime, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Logger.LogError("Communication error: {ErrorMessage}", ex.Message);
                        RequestSocketReconnect(ex);
                    }
                }

                if (_shouldReconnectSocket)
                {
                    await ReconnectSocketAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Fatal error in communication-task: {ErrorMessage}", ex.Message);
            }
        }

        IsConnected = false;
        Logger.LogInformation("Stopped communication-task for {Name}", Name);
    }
}