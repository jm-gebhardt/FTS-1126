using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.ConveyorTelegramApi.Processors;

/// <summary>
/// Provides a processor for telegrams received from the WE plc.
/// </summary>
[PublicAPI]
public class FromWePlcTelegramProcessor : InboundTelegramProcessorBase<ConveyorTelegram>
{
    private readonly ILogger<FromWePlcTelegramProcessor> _logger;
    private readonly TelegramProcessorFactory _processorFactory;

    /// <inheritdoc cref="FromWePlcTelegramProcessor"/>>
    public FromWePlcTelegramProcessor(ILogger<FromWePlcTelegramProcessor> logger, TelegramProcessorFactory processorFactory, [FromKeyedServices(ServiceKeyProvider.FromWeStation)] TelegramCommunicationConfiguration configuration) : base(configuration)
    {
        _logger = logger;
        RestartRequested = false;
        _processorFactory = processorFactory;
    }

    private bool SendAcknowledgement { get; set; }

    /// <summary>
    /// The current telegram processor.
    /// </summary>
    public ITelegramProcessor? CurrentProcessor { get; private set; }

    /// <summary>
    /// The current telegram.
    /// </summary>
    public ConveyorTelegram? CurrentTelegram { get; private set; }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
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

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool ShouldReceiveTelegram()
    {
        return !SendAcknowledgement;
    }

    private void HandleResetTelegram(ConveyorTelegram conveyorTelegram)
    {
        SendAcknowledgement = true;
        CurrentProcessor = null;
        CurrentTelegram = conveyorTelegram;
    }

    private void SkipTelegram()
    {
        _logger.LogInformation("Resent already processed telegram, skipping");
        SendAcknowledgement = true;
        SequenceCounter.Reset(CurrentTelegram!.Seq);
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        _logger.LogDebug("Received telegram {Telegram}", telegram);

        var conveyorTelegram = new ConveyorTelegram();
        if (!conveyorTelegram.Load(telegram))
        {
            _logger.LogError("Failed to load telegram {Telegram}", telegram);
            return;
        }
        _logger.LogDebug("Converted to telegram {Telegram}", conveyorTelegram);
        if (conveyorTelegram.Seq == 0)
        {
            HandleResetTelegram(conveyorTelegram);
            return;
        }

        if (CurrentTelegram != null && CurrentTelegram.Seq == conveyorTelegram.Seq)
        {
            SkipTelegram();
            return;
        }

        CurrentProcessor = _processorFactory.GetReceiveProcessor(conveyorTelegram);
        if (CurrentProcessor != null)
        {
            _logger.LogDebug("Sending telegram {Telegram} to processor", telegram);

            CurrentProcessor.ProcessReceivedTelegram(telegram);
            SendAcknowledgement = true;
            CurrentTelegram = conveyorTelegram;
            return;
        }
        _logger.LogError("Could not process telegram");
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void Cancel()
    {
        CurrentProcessor?.Cancel();
        CurrentTelegram = null;
        CurrentProcessor = null;
        SendAcknowledgement = false;
        RestartRequested = true;
    }
}