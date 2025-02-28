using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;

/// <summary>
/// Provides a telegram processor for <see cref="PosStateReq"/> telegrams
/// </summary>
public class PosStateReqProcessor(IBus bus, ILogger<PosStateReqProcessor> logger, [FromKeyedServices(ServiceKeyProvider.FromPrismat)] TelegramCommunicationConfiguration configuration) : InboundTelegramProcessorBase<TordDelete>(configuration)
{
    /// <summary>
    /// Current telegram
    /// </summary>
    private PosStateReq? CurrentTelegram { get; set; }

    /// <inheritdoc cref="InboundTelegramProcessorBase{PosStateReq}"/>>
    public override bool GetNextTelegramToSend(out string telegram)
    {
        if (CurrentTelegram == null)
        {
            telegram = string.Empty;
            return false;
        }
        telegram = CurrentTelegram!.CreateAcknowledgement();
        ShouldReceive = true;
        return true;
    }

    /// <inheritdoc cref="InboundTelegramProcessorBase{PosStateReq}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        CurrentTelegram = null;
        var posStateReq = new PosStateReq();
        if (!posStateReq.Load(telegram))
        {
            logger.LogError("Could not load PosStateReq telegram {Telegram}", telegram);
            return;
        }
        logger.LogInformation("Processing PosStateReq {PosStateReq}", posStateReq);

        CurrentTelegram = posStateReq;
        bus.Publish(new GetSystemStatus());
        ShouldReceive = false;
    }

    /// <inheritdoc cref="InboundTelegramProcessorBase{PosStateReq}"/>>
    public override void Cancel()
    {
        CurrentTelegram = null;
    }
}