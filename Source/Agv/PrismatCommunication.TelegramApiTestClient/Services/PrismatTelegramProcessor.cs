using System.Collections.Concurrent;
using System.Text;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApiClient.Services;

/// <summary>
/// Processor for incoming or outgoing telegrams
/// </summary>
public class PrismatTelegramProcessor : TelegramProcessorBase<PrismatTelegram>
{
    private readonly ILogger<PrismatTelegramProcessor> _logger;
    private readonly bool _isSender;

    /// <inheritdoc cref="PrismatTelegramProcessor"/>
    public PrismatTelegramProcessor(ILogger<PrismatTelegramProcessor> logger, bool startWithReceiving)
    {
        _logger = logger;
        ReceiveAcknowledgement = startWithReceiving;
        _isSender = !startWithReceiving;
    }

    private SeqCounter SequenceCounter { get; set; } = new(1, 999, 3);

    private bool ReceiveAcknowledgement { get; set; }

    private ConcurrentQueue<PrismatTelegram> SendQueue { get; } = new();

    private PrismatTelegram? CurrentTelegram { get; set; }

    [PublicAPI]
    private DateTime LastTelegramSend { get; set; }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override bool EnqueueTelegram(PrismatTelegram telegram)
    {
        SendQueue.Enqueue(telegram);
        return true;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override bool GetNextTelegramToSend(out string telegram)
    {
        if (SendQueue.IsEmpty && !ReceiveAcknowledgement && CurrentTelegram != null)
        {
            telegram = CurrentTelegram!.CreateAcknowledgement();
            _logger.LogInformation("Sending next acknowledgement {Next}", telegram);
            ReceiveAcknowledgement = true;
            return true;
        }

        if (SendQueue.TryDequeue(out var nextTelegram))
        {
            nextTelegram.Seq = SequenceCounter.Current;

            telegram = nextTelegram.ToString()!;
            _logger.LogInformation("Sending next telegram {Next} from sender {IsSender}", telegram, _isSender);

            ReceiveAcknowledgement = true;
            CurrentTelegram = nextTelegram;
            LastTelegramSend = DateTime.Now;
            SequenceCounter.Increase();
            return true;
        }

        telegram = string.Empty;
        return false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override bool ShouldReceiveTelegram()
    {
        return ReceiveAcknowledgement;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        var prismatTelegram = new PrismatTelegram();
        prismatTelegram.Load(telegram);

        _logger.LogInformation("Processed telegram {Telegram} from sender {IsSender}", telegram, _isSender);
        if (CurrentTelegram != null && prismatTelegram.Seq == CurrentTelegram.Seq && prismatTelegram.Func == CurrentTelegram!.Func && prismatTelegram.Rec == CurrentTelegram.Send && _isSender)
        {
            CurrentTelegram = null;
        }
        else
        {
            CurrentTelegram = prismatTelegram;
        }
        ReceiveAcknowledgement = false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override int GetTelegramLength()
    {
        return 0;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override int GetWaitingTime()
    {
        return 180000;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override bool ShouldRestart()
    {
        return false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override void RestartConnection()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override void Cancel()
    {
        CurrentTelegram = null;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override void ResetCounter()
    {
        SequenceCounter = new SeqCounter(1, 999, 3);
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override bool HasSendDemand()
    {
        return SendQueue.Count > 0;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override Encoding GetEncoding()
    {
        return Encoding.UTF8;
    }
}