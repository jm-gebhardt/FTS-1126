using System.Text;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserConveyorTelegramApiServer.Services;

/// <summary>
/// Provides a processor for incoming conveyor telegrams.
/// </summary>
public class ConveyorTelegramProcessor : TelegramProcessorBase<ConveyorTelegram>
{
    private readonly ILogger<ConveyorTelegramProcessor> _logger;
    private readonly TelegramCommunicationConfiguration _configuration;

    /// <summary>
    /// Provides a processor for incoming conveyor telegrams.
    /// </summary>
    public ConveyorTelegramProcessor(ILogger<ConveyorTelegramProcessor> logger, TelegramCommunicationConfiguration configuration)
    {
        _logger = logger;
        RestartRequested = false;
        _configuration = configuration;
    }

    private ConveyorTelegram? CurrentTelegram { get; set; }

    private bool SendAcknowledgement { get; set; }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool EnqueueTelegram(ConveyorTelegram telegram)
    {
        // not applicable for inbound telegrams
        return true;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool GetNextTelegramToSend(out string telegram)
    {
        if (CurrentTelegram != null && SendAcknowledgement)
        {
            telegram = CurrentTelegram!.CreateAcknowledgement();
            _logger.LogInformation("Sending acknowledgement {Telegram}", telegram);
            SendAcknowledgement = false;
            return true;
        }
        SendAcknowledgement = false;
        telegram = string.Empty;
        return false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool ShouldReceiveTelegram()
    {
        return !SendAcknowledgement;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        var conveyorTelegram = new ConveyorTelegram();
        conveyorTelegram.Load(telegram);
        _logger.LogInformation("Processing telegram {Telegram}", telegram);

        CurrentTelegram = conveyorTelegram;
        SendAcknowledgement = true;
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
        RestartRequested = true;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override void ResetCounter()
    {
        // not applicable for inbound telegrams
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool GetUseLifeTelegrams()
    {
        return _configuration.UseLifeTelegrams;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override bool HasSendDemand()
    {
        return false;
    }

    /// <inheritdoc cref="TelegramProcessorBase{ConveyorTelegram}"/>>
    public override Encoding GetEncoding()
    {
        return Encoding.UTF8;
    }
}