using System.Collections.Concurrent;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;

/// <summary>
/// Provides a processor for telegrams sent to the conveyor
/// </summary>
public class ToWePlcTelegramProcessor : TelegramProcessorBase<ConveyorTelegram>
{
    private bool ReceiveAcknowledgement { get; set; }

    /// <summary>
    ///  The send queue.
    /// </summary>
    private ConcurrentQueue<ConveyorTelegram> SendQueue { get; }

    /// <summary>
    ///  The send sequence counter.
    /// </summary>
    public SeqCounter SequenceCounter { get; private set; } = new(1, 999, 3);

    /// <summary>
    /// The current telegram.
    /// </summary>
    public ConveyorTelegram? CurrentTelegram { get; private set; }

    /// <summary>
    /// Timestamp when last telegram was send.
    /// </summary>
    public DateTimeOffset LastTelegramSend { get; private set; }

    /// <summary>
    /// Start of waiting for acknowledgement
    /// </summary>
    public DateTimeOffset AwaitAcknowledgementStart { get; private set; }

    /// <summary>
    /// Maximum time to wait for acknowledgements [ms]
    /// </summary>
    public int MaxAcknowledgementWaitTime => _configuration.TelegramWaitingTime;

    /// <summary>
    /// Maximum number of retries for sending telegrams
    /// </summary>
    public int MaxRetryAttempts => 3;

    /// <summary>
    /// Remaining attempts for resending
    /// </summary>
    public int RemainingAttempts { get; private set; }

    private int _acknowledgementWaitTime;
    private readonly ILogger<ToWePlcTelegramProcessor> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TelegramCommunicationConfiguration _configuration;

    /// <summary>
    /// Provides a processor for telegrams sent to the conveyor
    /// </summary>
    public ToWePlcTelegramProcessor(ILogger<ToWePlcTelegramProcessor> logger, TimeProvider timeProvider, TelegramCommunicationConfiguration configuration, ConcurrentQueue<ConveyorTelegram> sendQueue)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        AwaitAcknowledgementStart = _timeProvider.GetUtcNow();
        _configuration = configuration;
        RestartRequested = false;
        SendQueue = sendQueue;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool EnqueueTelegram(ConveyorTelegram telegram)
    {
        SendQueue.Enqueue(telegram);
        return true;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool GetNextTelegramToSend(out string telegram)
    {
        if (CurrentTelegram != null && CurrentTelegram.Seq == SequenceCounter.Current && RemainingAttempts > 0)
        {
            _logger.LogInformation("Resending telegram {Telegram}, remaining attempts {RemainingAttempts}", CurrentTelegram.ToString(), RemainingAttempts);
            telegram = CurrentTelegram!.ToString()!;
            LastTelegramSend = _timeProvider.GetUtcNow();
            RemainingAttempts -= 1;
            AwaitAcknowledgementStart = _timeProvider.GetUtcNow();
            _acknowledgementWaitTime = MaxAcknowledgementWaitTime;
            ReceiveAcknowledgement = true;
            return true;
        }

        if (CurrentTelegram != null && CurrentTelegram.Seq == SequenceCounter.Current && RemainingAttempts <= 0)
        {
            _logger.LogInformation("No more remaining attempts for resending telegram {Telegram}, trigger restart", CurrentTelegram.ToString());
            RestartRequested = true;
        }

        if (!HasSendDemand())
        {
            TrySendLifeTelegram();
        }

        if (SendQueue.TryDequeue(out var nextTelegram))
        {
            nextTelegram.Seq = SequenceCounter.Current;
            telegram = nextTelegram.ToString()!;
            _logger.LogInformation("ToConveyorProcessor: Sending next telegram {Next}", telegram);
            ReceiveAcknowledgement = true;
            CurrentTelegram = nextTelegram;
            LastTelegramSend = _timeProvider.GetUtcNow();
            RemainingAttempts = MaxRetryAttempts;
            AwaitAcknowledgementStart = _timeProvider.GetUtcNow();
            _acknowledgementWaitTime = MaxAcknowledgementWaitTime;
            return true;
        }

        ReceiveAcknowledgement = false;
        telegram = string.Empty;
        return false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool ShouldReceiveTelegram()
    {
        var awaitAcknowledgementStart = _timeProvider.GetUtcNow() - AwaitAcknowledgementStart;
        _acknowledgementWaitTime = MaxAcknowledgementWaitTime - (int) awaitAcknowledgementStart.TotalMilliseconds;
        return ReceiveAcknowledgement && _acknowledgementWaitTime > 0;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        _logger.LogInformation("Receiving telegram {Telegram}", telegram);
        var acknowledgement = new ConveyorTelegram();
        acknowledgement.Load(telegram);
        if (acknowledgement.Seq == CurrentTelegram!.Seq && ReceiveAcknowledgement)
        {
            ReceiveAcknowledgement = false;
            SequenceCounter.Increase();
            CurrentTelegram = null;
        }
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override int GetTelegramLength()
    {
        // Enforce usage of length independent receive method
        return 0;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override int GetWaitingTime()
    {
        return _configuration.TelegramWaitingTime;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void Cancel()
    {
        if (!SendQueue.IsEmpty)
        {
            _logger.LogWarning("Cancel requested, but there are {Count} messages pending send", SendQueue.Count);
        }
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void ResetCounter()
    {
        SequenceCounter = new SeqCounter(1, 999, 3);
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool GetUseLifeTelegrams()
    {
        return _configuration.UseLifeTelegrams;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool HasSendDemand()
    {
        bool shouldResend = CurrentTelegram != null && CurrentTelegram.Seq == SequenceCounter.Current && !ReceiveAcknowledgement && RemainingAttempts > 0;
        return SendQueue.Count > 0 || shouldResend;
    }

    /// <summary>
    /// Send life telegram
    /// </summary>
    public void TrySendLifeTelegram()
    {
        if (_timeProvider.GetUtcNow() - LastTelegramSend > TimeSpan.FromSeconds(_configuration.LifeTelegramInterval) && !ShouldReceiveTelegram())
        {
            SendQueue.Enqueue(new Life(_timeProvider.GetUtcNow().UtcDateTime));
        }
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override Encoding GetEncoding()
    {
        return Encoding.UTF8;
    }
}