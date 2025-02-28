using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Services;

/// <inheritdoc cref="IConveyorCommunicator"/>>
[PublicAPI]
public class TelegramConveyorCommunicator(ILogger<TelegramConveyorCommunicator> logger, [FromKeyedServices(ServiceKeyProvider.AplWeStation)] ToConveyorTelegramProcessor aplWeStationTelegramProcessor, [FromKeyedServices(ServiceKeyProvider.AgvCargoState)] ToConveyorTelegramProcessor agvCargoStateTelegramProcessor) : IConveyorCommunicator
{
    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportAplCargoState(bool cargo)
    {
        var agvAplCargoState = new AgvAplCargoState(cargo, DateTime.UtcNow);
        agvCargoStateTelegramProcessor.EnqueueTelegram(agvAplCargoState);
        logger.LogInformation("Queuing telegram '{Telegram}' for sending to conveyor server {Server}", agvAplCargoState.ToString(), ServiceKeyProvider.AgvCargoState);
    }
    
    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportPalletPickDropStation(string targetStation, string? pickStation, string? dropStation)
    {
        var agvWeStation = new AgvWeStation(targetStation, dropStation, pickStation, DateTime.UtcNow);
        aplWeStationTelegramProcessor.EnqueueTelegram(agvWeStation);
        logger.LogInformation("Queuing telegram '{Telegram}' for sending to conveyor server {Server}", agvWeStation.ToString(), ServiceKeyProvider.AplWeStation);
    }
}
