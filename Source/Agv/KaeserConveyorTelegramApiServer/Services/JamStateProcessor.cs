using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Life = Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams.Life;

namespace Gebhardt.Acc.KaeserConveyorTelegramApiServer.Services;

/// <summary>
/// Processor for jam state telegrams
/// </summary>
public class JamStateProcessor : TelegramProcessorBase<ConveyorTelegram>
{
    private readonly ILogger<JamStateProcessor> _logger;
    private readonly TelegramCommunicationConfiguration _configuration;

    private int RemainingAttempts { get; set; }

    /// <summary>
    /// Maximum number of retries for sending telegrams
    /// </summary>
    private const int MaxRetryAttempts = 3;

    private SeqCounter SequenceCounter { get; set; } = new(1, 999, 3);

    private bool ReceiveAcknowledgement { get; set; }

    private ConcurrentQueue<ConveyorTelegram> SendQueue { get; } = new();

    private ConveyorTelegram? CurrentTelegram { get; set; }

    [PublicAPI]
    private DateTime LastTelegramSend { get; set; }

    /// <inheritdoc cref="JamStateProcessor"/>
    public JamStateProcessor(ILogger<JamStateProcessor> logger, [FromKeyedServices(ServiceKeyProvider.FromWeStation)]TelegramCommunicationConfiguration configuration)
    {
        _logger=logger;
        _configuration=configuration;
        ReceiveAcknowledgement = false;
        RestartRequested = false;
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
        if (SendQueue.IsEmpty && !ReceiveAcknowledgement && CurrentTelegram != null && RemainingAttempts > 0)
        {
            telegram = CurrentTelegram!.CreateAcknowledgement();
            _logger.LogInformation("Sending next acknowledgement {Next}", telegram);
            ReceiveAcknowledgement = true;
            RemainingAttempts -= 1;
            return true;
        }

        if (CurrentTelegram != null && CurrentTelegram.Seq == SequenceCounter.Current && RemainingAttempts <= 0)
        {
            RestartRequested = true;
            ReceiveAcknowledgement = false;
        }

        if (!HasSendDemand())
        {
            TrySendLifeTelegram();
        }

        if (SendQueue.TryDequeue(out var nextTelegram))
        {
            nextTelegram.Seq = SequenceCounter.Current;

            telegram = nextTelegram.ToString()!;
            _logger.LogInformation("Sending next telegram {Next} ", telegram);

            ReceiveAcknowledgement = true;
            CurrentTelegram = nextTelegram;
            LastTelegramSend = DateTime.Now;
            SequenceCounter.Increase();
            RemainingAttempts = MaxRetryAttempts;
            return true;
        }

        telegram = string.Empty;
        return false;
    }

    /// <summary>
    /// Send life telegram
    /// </summary>
    private void TrySendLifeTelegram()
    {
        bool isTimeToSendLifeTelegram = DateTime.Now - LastTelegramSend > TimeSpan.FromSeconds(_configuration.LifeTelegramInterval);
        if (isTimeToSendLifeTelegram && !ShouldReceiveTelegram())
        {
            SendQueue.Enqueue(new Life(DateTime.UtcNow));
        }
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool ShouldReceiveTelegram()
    {
        return ReceiveAcknowledgement;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        var conveyorTelegram = new ConveyorTelegram();
        conveyorTelegram.Load(telegram);
        _logger.LogInformation("Processed telegram {Telegram} ", telegram);
        if (CurrentTelegram != null && conveyorTelegram.Seq == CurrentTelegram.Seq && conveyorTelegram.Func == CurrentTelegram!.Func && conveyorTelegram.Rec == CurrentTelegram.Send)
        {
            CurrentTelegram = null;
        }
        
        ReceiveAcknowledgement = false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override int GetTelegramLength()
    {
        return 0;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override int GetWaitingTime()
    {
        return _configuration.TelegramWaitingTime;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool HasSendDemand()
    {
        return SendQueue.Count > 0;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void Cancel()
    {
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
    public override Encoding GetEncoding()
    {
        return Encoding.UTF8;
    }
}