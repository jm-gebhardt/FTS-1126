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
public class JamStateCommandHandler(ILogger<JamStateCommandHandler> logger, IJamStateRepository jamStateRepository, IConveyorCommunicator communicator): 
    IConsumer<UpdateJamState>,
    IConsumer<RequestJamState>
{
    /// <summary>
    /// Handles the <see cref="UpdateJamState"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateJamState> context)
    {
        (string station, int state, var timeStamp) = context.Message;

        var jamState = await jamStateRepository.GetByStationAsync(station);
        if (jamState == null)
        {
            jamState = new JamState(station, state, timeStamp);
            logger.LogInformation("Adding jam state {State} for station {Station}", jamState.State, jamState.Station);
            await jamStateRepository.AddAsync(jamState);
            return;
        }

        jamState.SetState(state, timeStamp);
        await jamStateRepository.UpdateAsync(jamState);
        logger.LogInformation("Updating jam state {State} for station {Station}", jamState.State, jamState.Station);
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