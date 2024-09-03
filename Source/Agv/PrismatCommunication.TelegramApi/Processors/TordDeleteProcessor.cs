using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;

/// <summary>
/// Provides a telegram processor for <see cref="TordDelete"/> telegrams
/// </summary>
public class TordDeleteProcessor(IBus bus, ILogger<TordDeleteProcessor> logger, [FromKeyedServices(ServiceKeyProvider.FromPrismat)] TelegramCommunicationConfiguration configuration) : InboundTelegramProcessorBase<TordDelete>(configuration)
{
    /// <summary>
    /// Current telegram
    /// </summary>
    public TordDelete? CurrentTelegram { get; private set; }

    /// <inheritdoc cref="InboundTelegramProcessorBase{TordDelete}"/>>
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

    /// <inheritdoc cref="InboundTelegramProcessorBase{TordDelete}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        CurrentTelegram = null;
        var tordDelete = new TordDelete();
        if (!tordDelete.Load(telegram))
        {
            logger.LogError("Could not load tordDelete telegram {Telegram}", telegram);
            return;
        }
        logger.LogInformation("Processing tordDelete {TordDelete}", tordDelete);

        CurrentTelegram = tordDelete;
        bus.Publish(new CancelHostOrder(tordDelete.Hu));
        ShouldReceive = false;
    }

    /// <inheritdoc cref="InboundTelegramProcessorBase{TordDelete}"/>>
    public override void Cancel()
    {
        CurrentTelegram = null;
    }
}
