using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;

/// <summary>
/// Provides a processor for telegrams sent to the conveyor
/// </summary>
public class ToConveyorTelegramProcessor(ILogger<ToConveyorTelegramProcessor> logger, ITimeProvider timeProvider, TelegramCommunicationConfiguration configuration) : TelegramProcessorBase<ConveyorTelegram>
{
    private bool ReceiveAcknowledgement { get; set; }

    private bool TriggerRestart { get; set; }

    /// <summary>
    ///  The send queue.
    /// </summary>
    private ConcurrentQueue<ConveyorTelegram> SendQueue { get; } = new();

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
    public DateTime LastTelegramSend { get; private set; }

    /// <summary>
    /// Start of waiting for acknowledgement
    /// </summary>
    public DateTime AwaitAcknowledgementStart { get; private set; }

    /// <summary>
    /// Maximum time to wait for acknowledgements [ms]
    /// </summary>
    public int MaxAcknowledgementWaitTime => configuration.TelegramWaitingTime;

    /// <summary>
    /// Maximum number of retries for sending telegrams
    /// </summary>
    public int MaxRetryAttempts => 3;

    /// <summary>
    /// Remaining attempts for resending
    /// </summary>
    public int RemainingAttempts { get; private set; }

    private int _acknowledgementWaitTime;

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
            logger.LogInformation("Resending telegram {Telegram}", CurrentTelegram.ToString());
            telegram = CurrentTelegram!.ToString()!;
            LastTelegramSend = timeProvider.Now();
            RemainingAttempts -= 1;
            AwaitAcknowledgementStart = timeProvider.Now();
            _acknowledgementWaitTime = MaxAcknowledgementWaitTime;
            ReceiveAcknowledgement = true;
            return true;
        }

        if (CurrentTelegram != null && CurrentTelegram.Seq == SequenceCounter.Current && RemainingAttempts <= 0)
        {
            TriggerRestart = true;
        }

        if (!HasSendDemand())
        {
            TrySendLifeTelegram();
        }

        if (SendQueue.TryDequeue(out var nextTelegram))
        {
            nextTelegram.Seq = SequenceCounter.Current;
            telegram = nextTelegram.ToString()!;
            logger.LogInformation("ToConveyorProcessor: Sending next telegram {Next}", telegram);
            ReceiveAcknowledgement = true;
            CurrentTelegram = nextTelegram;
            LastTelegramSend = timeProvider.Now();
            RemainingAttempts = MaxRetryAttempts;
            AwaitAcknowledgementStart = timeProvider.Now();
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
        _acknowledgementWaitTime = MaxAcknowledgementWaitTime - (int)(timeProvider.Now() - AwaitAcknowledgementStart).TotalMilliseconds;
        return ReceiveAcknowledgement && _acknowledgementWaitTime > 0;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        logger.LogInformation("Receiving telegram {Telegram}", telegram);
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
        return configuration.TelegramWaitingTime;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool ShouldRestart()
    {
        return TriggerRestart;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void RestartConnection()
    {
        TriggerRestart = false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void Cancel()
    {
        if (!SendQueue.IsEmpty)
        {
            logger.LogWarning("Cancel requested, but there are {Count} messages pending send", SendQueue.Count);
        }
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void ResetCounter()
    {
        SequenceCounter = new SeqCounter(1, 999, 3);
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
        if (DateTime.UtcNow - LastTelegramSend > TimeSpan.FromSeconds(configuration.LifeTelegramInterval) && !ShouldReceiveTelegram())
        {
            SendQueue.Enqueue(new Life(DateTime.UtcNow));
        }
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override Encoding GetEncoding()
    {
        return Encoding.UTF8;
    }
}