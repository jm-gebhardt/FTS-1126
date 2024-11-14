using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Services;

/// <inheritdoc cref="IConveyorCommunicator"/>>
[PublicAPI]
public class TelegramConveyorCommunicator(ILogger<TelegramConveyorCommunicator> logger, ToConveyorTelegramProcessor telegramProcessor) : IConveyorCommunicator
{
    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportAplCargoState(bool cargo)
    {
        var agvAplCargoState = new AgvAplCargoState(cargo, DateTime.UtcNow);
        telegramProcessor.EnqueueTelegram(agvAplCargoState);
        logger.LogInformation("Queuing telegram '{Telegram}' for sending to conveyor", agvAplCargoState.ToString());
    }
    
    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportPalletPickDropStation(string targetStation, string? pickStation, string? dropStation)
    {
        var agvWeStation = new AgvWeStation(targetStation, dropStation, pickStation, DateTime.UtcNow);
        telegramProcessor.EnqueueTelegram(agvWeStation);
        logger.LogInformation("Queuing telegram '{Telegram}' for sending to conveyor", agvWeStation.ToString());
    }
}
