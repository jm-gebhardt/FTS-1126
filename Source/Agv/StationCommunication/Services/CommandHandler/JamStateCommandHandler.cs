using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Domain.Services;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Services.CommandHandler;

/// <summary>
/// Provides a handler for jam state related events.
/// </summary>
[UsedImplicitly]
public class JamStateCommandHandler(ILogger<JamStateCommandHandler> logger, IJamStateRepository jamStateRepository, IConveyorCommunicator communicator) :
    IConsumer<UpdateJamState>,
    IConsumer<RequestJamState>
{
    /// <inheritdoc cref="JamStateCommandHandler"/>
    public async Task Consume(ConsumeContext<UpdateJamState> context)
    {
        (var stationId, int state, var timeStamp) = context.Message;

        var jamState = await jamStateRepository.GetByStationAsync(stationId);
        if (jamState == null)
        {
            jamState = new JamState(stationId, state, timeStamp);
            logger.LogInformation("Adding jam state {State} for station {StationId}", jamState.State, jamState.StationId);
            await jamStateRepository.AddAsync(jamState);
            await context.Publish(new JamStateUpdated(jamState.StationId, state == 1));
            return;
        }
        
        await context.Publish(new JamStateUpdated(jamState.StationId, state == 1));

        jamState.SetState(state, timeStamp);
        await jamStateRepository.UpdateAsync(jamState);
        logger.LogInformation("Updating jam state {State} for station {StationId}", jamState.State, jamState.StationId);
    }

    /// <inheritdoc cref="JamStateCommandHandler"/>
    public Task Consume(ConsumeContext<RequestJamState> context)
    {
        communicator.RequestStationState(context.Message.Station);
        return Task.CompletedTask;
    }
}