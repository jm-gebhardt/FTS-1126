using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.ConveyorTelegramApi.Processors;

/// <summary>
/// Provides a processor for the <see cref="JamState"/> telegram
/// </summary>
[UsedImplicitly]
public class JamStateProcessor(ILogger<JamStateProcessor> logger, [FromKeyedServices(ServiceKeyProvider.FromWeStation)] TelegramCommunicationConfiguration configuration, IBus bus) : InboundTelegramProcessorBase<JamState>(configuration)
{
    /// <summary>
    /// Current telegram
    /// </summary>
    public JamState? CurrentTelegram { get; private set; }

    /// <inheritdoc cref="InboundTelegramProcessorBase{JamState}"/>>
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

    /// <inheritdoc cref="InboundTelegramProcessorBase{JamState}"/>>
    public override void ProcessReceivedTelegram(string telegram)
    {
        CurrentTelegram = null;
        var jamState = new JamState();
        if (!jamState.Load(telegram))
        {
            logger.LogError("Could not load tordDelete telegram {Telegram}", telegram);
            return;
        }
        logger.LogInformation("Processing telegram {JamState}", jamState);

        CurrentTelegram = jamState;
        bus.Publish(new JamStateReceived(jamState.Pos, jamState.State, DateTime.UtcNow));
        ShouldReceive = false;
    }

    /// <inheritdoc cref="InboundTelegramProcessorBase{JamState}"/>>
    public override void Cancel()
    {
        CurrentTelegram = null;
    }
}