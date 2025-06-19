using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.SimulationMode;

/// <inheritdoc cref="IConveyorCommunicator"/>>
public class SimulationConveyorCommunicator(ILogger<SimulationConveyorCommunicator> logger) : IConveyorCommunicator
{
    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportAplCargoState(bool cargo)
    {
        var agvAplCargoState = new AgvAplCargoState(cargo, DateTime.UtcNow);
        logger.LogInformation("Queuing telegram '{Telegram}' for sending to conveyor", agvAplCargoState.ToString());
    }
    
    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportPalletPickDropStation(string targetStation, string? pickStation, string? dropStation, string? hu)
    {
        var agvWeStation = new AgvWeStation(targetStation, dropStation, pickStation, hu, DateTime.UtcNow);
        logger.LogInformation("Queuing telegram '{Telegram}' for sending to conveyor", agvWeStation.ToString());
    }

    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void ReportPalletDrop(string source,string destination, string hu)
    {
        var tord = new Tord(source, destination, hu, DateTime.UtcNow);
        logger.LogDebug("Queuing telegram '{Telegram}' for sending to conveyor", tord.ToString());
    }

    /// <inheritdoc cref="IConveyorCommunicator"/>>
    public void RequestStationState(string station)
    {
        var posStateReq = new PosStateReq(station, DateTime.UtcNow);
        logger.LogInformation("Queuing telegram '{Telegram}' for sending to conveyor", posStateReq.ToString());
    }
}