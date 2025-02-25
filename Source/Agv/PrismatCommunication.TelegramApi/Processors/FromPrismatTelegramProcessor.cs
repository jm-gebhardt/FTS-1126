using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;

/// <summary>
/// Provides a processor for telegrams coming from prismat
/// </summary>
public class FromPrismatTelegramProcessor : InboundTelegramProcessorBase<PrismatTelegram>
{
    private readonly TelegramProcessorFactory _processorFactory;
    private readonly ILogger<FromPrismatTelegramProcessor> _logger;

    /// <summary>
    /// Provides a processor for telegrams coming from prismat
    /// </summary>
    public FromPrismatTelegramProcessor(TelegramProcessorFactory processorFactory, ILogger<FromPrismatTelegramProcessor> logger, [FromKeyedServices(ServiceKeyProvider.FromPrismat)] TelegramCommunicationConfiguration configuration) : base(configuration)
    {
        _processorFactory = processorFactory;
        _logger = logger;
        TriggerRestart = false;
    }

    private bool SendAcknowledgement { get; set; }

    /// <summary>
    /// The current telegram processor.
    /// </summary>
    public ITelegramProcessor? CurrentProcessor { get; private set; }

    /// <summary>
    /// The current telegram.
    /// </summary>
    public PrismatTelegram? CurrentTelegram { get; private set; }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override bool GetNextTelegramToSend(out string telegram)
    {
        if (CurrentProcessor != null)
        {
            if (CurrentProcessor!.GetNextTelegramToSend(out string? currentTelegram))
            {
                _logger.LogInformation("Sending next telegram {Next}", currentTelegram);

                telegram = currentTelegram!;
                SendAcknowledgement = false;
                SequenceCounter.Increase();
                return true;
            }
            telegram = string.Empty;
            return false;
        }

        if (CurrentTelegram == null)
        {
            telegram = string.Empty;
            return false;
        }

        SendAcknowledgement = false;
        telegram = CurrentTelegram!.CreateAcknowledgement();
        SequenceCounter.Increase();
        return true;
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override bool ShouldReceiveTelegram()
    {
        return !SendAcknowledgement;
    }

    private void HandleResetTelegram(PrismatTelegram prismatTelegram)
    {
        SendAcknowledgement = true;
        CurrentProcessor = null;
        CurrentTelegram = prismatTelegram;
    }

    private void SkipTelegram()
    {
        _logger.LogInformation("Resent already processed telegram, skipping");
        SendAcknowledgement = true;
        SequenceCounter.Reset(CurrentTelegram!.Seq);
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        _logger.LogInformation("Received telegram {Telegram}", telegram);

        var prismatTelegram = new PrismatTelegram();
        if (!prismatTelegram.Load(telegram))
        {
            _logger.LogInformation("Failed to load telegram {Telegram}", telegram);
            return;
        }
        _logger.LogInformation("Converted to telegram {Telegram}", prismatTelegram);
        if (prismatTelegram.Seq == 0)
        {
            HandleResetTelegram(prismatTelegram);
            return;
        }

        if (CurrentTelegram != null && CurrentTelegram.Seq == prismatTelegram.Seq)
        {
            SkipTelegram();
            return;
        }

        CurrentProcessor = _processorFactory.GetReceiveProcessor(prismatTelegram);
        if (CurrentProcessor != null)
        {
            _logger.LogInformation("Sending it to processor");

            CurrentProcessor.ProcessReceivedTelegram(telegram);
            SendAcknowledgement = true;
            CurrentTelegram = prismatTelegram;
            return;
        }
        _logger.LogError("Could not process telegram");
    }

    /// <inheritdoc cref="TelegramProcessorBase{PrismatTelegram}"/>>
    public override void Cancel()
    {
        CurrentProcessor?.Cancel();
        CurrentTelegram = null;
        CurrentProcessor = null;
        SendAcknowledgement = false;
        TriggerRestart = true;
    }
}