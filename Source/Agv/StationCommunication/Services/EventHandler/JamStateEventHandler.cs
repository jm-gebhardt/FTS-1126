using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Services.EventHandler;

/// <summary>
/// Provides a handler for jam state related events.
/// </summary>
[UsedImplicitly]
public class JamStateEventHandler(IStationRepository stationRepository) :
    IConsumer<JamStateReceived>,
    IConsumer<StationUnlocked>,
    IConsumer<JamStateRequested>
{
    /// <inheritdoc cref="JamStateEventHandler"/>
    public async Task Consume(ConsumeContext<JamStateReceived> context)
    {
        (string station, int state, var timeStamp) = context.Message;
        await context.Publish(new UpdateJamState(station.ToGuid(), state, timeStamp));
    }

    /// <inheritdoc cref="JamStateEventHandler"/>
    public async Task Consume(ConsumeContext<StationUnlocked> context)
    {
        var unlockedStation = context.Message.StationNode;
        var station = await stationRepository.GetStationByName(unlockedStation.Name);
        if (station is { HasJamState: true })
        {
            await context.Publish(new RequestJamState(station.Name));
        }
    }

    /// <inheritdoc cref="JamStateEventHandler"/>
    public async Task Consume(ConsumeContext<JamStateRequested> context)
    {
        var stationNode = context.Message.StationNode;
        var station = await stationRepository.GetStationByName(stationNode.Name);
        if (station != null)
        {
            await context.Publish(new RequestJamState(station.Name));
        }
    }
}