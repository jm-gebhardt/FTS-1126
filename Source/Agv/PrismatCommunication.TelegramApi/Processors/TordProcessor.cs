using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;

/// <summary>
/// Provides a telegram processor for <see cref="Tord"/> telegrams
/// </summary>
public class TordProcessor(IBus bus, ILogger<TordProcessor> logger, [FromKeyedServices(ServiceKeyProvider.FromPrismat)]  TelegramCommunicationConfiguration configuration) : InboundTelegramProcessorBase<Tord>(configuration)
{
    /// <summary>
    /// Current telegram
    /// </summary>
    public Tord? CurrentTelegram { get; private set; }

    /// <inheritdoc cref="InboundTelegramProcessorBase{Tord}"/>>
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

    /// <inheritdoc cref="InboundTelegramProcessorBase{Tord}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        CurrentTelegram = null;
        var tord = new Tord();
        if (!tord.Load(telegram))
        {
            logger.LogError("Could not load tord telegram {Telegram}", telegram);
            return;
        }
        logger.LogInformation("Processing tord {Tord}", tord);

        CurrentTelegram = tord;
        bus.Publish(new CreateHostOrder(tord.Hu, new OrderDetails(tord.Hu.ToGuid(), tord.OId, tord.Src, tord.Dest, tord.Hu, HostOrderCreator.Wms, tord.Time, tord.HuRot)));
        ShouldReceive = false;
    }

    /// <inheritdoc cref="InboundTelegramProcessorBase{Tord}"/>>
    public override void Cancel()
    {
        CurrentTelegram = null;
    }
}