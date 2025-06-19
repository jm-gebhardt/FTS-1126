using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;

/// <summary>
/// Provides a telegram processor for <see cref="Life"/> telegrams
/// </summary>
public class LifeProcessor(ILogger<LifeProcessor> logger, [FromKeyedServices(ServiceKeyProvider.FromPrismat)]  TelegramCommunicationConfiguration configuration) : InboundTelegramProcessorBase<Life>(configuration)
{
    /// <summary>
    /// The current telegram
    /// </summary>
    public Life? CurrentTelegram { get; private set; }

    /// <inheritdoc cref="InboundTelegramProcessorBase{Life}"/>>
    public override bool GetNextTelegramToSend(out string telegram)
    {
        if (CurrentTelegram == null)
        {
            telegram = string.Empty;
            ShouldReceive = true;
            return false;
        }
        telegram = CurrentTelegram!.CreateAcknowledgement();
        ShouldReceive = true;
        return true;
    }

    /// <inheritdoc cref="InboundTelegramProcessorBase{Life}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        var life = new Life(DateTime.UtcNow);
        if (!life.Load(telegram))
        {
            logger.LogError("Could not load life telegram");
            return;
        }
        CurrentTelegram = life;
        ShouldReceive = false;
    }

    /// <inheritdoc cref="InboundTelegramProcessorBase{Life}"/>>
    public override void Cancel()
    {
        CurrentTelegram = null;
    }
}