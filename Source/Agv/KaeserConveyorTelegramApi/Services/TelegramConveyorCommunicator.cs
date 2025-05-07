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
public class TelegramConveyorCommunicator(ILogger<TelegramConveyorCommunicator> logger, [FromKeyedServices(ServiceKeyProvider.AplWeStation)] ToWePlcTelegramProcessor toWePlcTelegramProcessor, [FromKeyedServices(ServiceKeyProvider.AgvCargoState)] AgvAplCargoStateProcessor agvCargoStateTelegramProcessor) : IConveyorCommunicator
{
    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportAplCargoState(bool cargo)
    {
        var agvAplCargoState = new AgvAplCargoState(cargo, DateTime.UtcNow);
        agvCargoStateTelegramProcessor.EnqueueTelegram(agvAplCargoState);
        logger.LogDebug("Queuing telegram '{Telegram}' for sending to conveyor server {Server}", agvAplCargoState.ToString(), ServiceKeyProvider.AgvCargoState);
    }
    
    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportPalletPickDropStation(string targetStation, string? pickStation, string? dropStation, string? hu)
    {
        var agvWeStation = new AgvWeStation(targetStation, dropStation, pickStation, hu, DateTime.UtcNow);
        toWePlcTelegramProcessor.EnqueueTelegram(agvWeStation);
        logger.LogDebug("Queuing telegram '{Telegram}' for sending to conveyor server {Server}", agvWeStation.ToString(), ServiceKeyProvider.AplWeStation);
    }

    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportPalletDrop(string source, string destination, string hu)
    {
        var tord = new Tord(source, destination, hu, DateTime.UtcNow);
        toWePlcTelegramProcessor.EnqueueTelegram(tord);
        logger.LogDebug("Queuing telegram '{Telegram}' for sending to conveyor server {Server}", tord.ToString(), ServiceKeyProvider.AplWeStation);
    }
}
