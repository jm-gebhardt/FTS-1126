using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Messages.JamState;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.CommandHandler;

/// <summary>
/// Provides a handler for jam state related events.
/// </summary>
[UsedImplicitly]
public class JamStateCommandHandler(ILogger<JamStateCommandHandler> logger, IJamStateRepository jamStateRepository, IConveyorCommunicator communicator) :
    IConsumer<UpdateJamState>,
    IConsumer<RequestJamState>
{
    /// <summary>
    /// Handles the <see cref="UpdateJamState"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateJamState> context)
    {
        (var stationId, int state, var timeStamp) = context.Message;

        var jamState = await jamStateRepository.GetByStationAsync(stationId);
        if (jamState == null)
        {
            jamState = new JamState(stationId, state, timeStamp);
            logger.LogInformation("Adding jam state {State} for station {StationId}", jamState.State, jamState.StationId);
            await jamStateRepository.AddAsync(jamState);
            return;
        }
        if (state != jamState.State)
        {
            await context.Publish(new JamStateUpdated(jamState.StationId, state == 1));
        }

        jamState.SetState(state, timeStamp);
        await jamStateRepository.UpdateAsync(jamState);
        logger.LogInformation("Updating jam state {State} for station {StationId}", jamState.State, jamState.StationId);
    }

    /// <summary>
    /// Handles the <see cref="RequestJamState"/> command.
    /// </summary>
    public Task Consume(ConsumeContext<RequestJamState> context)
    {
        communicator.RequestStationState(context.Message.Station);
        return Task.CompletedTask;
    }
}