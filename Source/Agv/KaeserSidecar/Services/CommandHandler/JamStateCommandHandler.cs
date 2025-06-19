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
    IConsumer<GetJamState>
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
    /// Handles the <see cref="GetJamState"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<GetJamState> context)
    {
        (string station, var requestTime, int waitingTimeSec, int outdatedTimeSec) = context.Message;

        communicator.RequestStationState(station);

        await Task.Delay(TimeSpan.FromSeconds(waitingTimeSec));

        var jamState = await jamStateRepository.GetByStationAsync(station) ?? new JamState(station, 0, DateTime.UtcNow);
        if ((requestTime - jamState.LastStateChangeTime).TotalSeconds > outdatedTimeSec)
        {
            logger.LogInformation("Jam state older than {OutDatedTime} seconds, waiting {WaitingTime} seconds for response", outdatedTimeSec, waitingTimeSec);
            await Task.Delay(TimeSpan.FromSeconds(waitingTimeSec));
            jamState = await jamStateRepository.GetByStationAsync(station) ?? new JamState(station, 0, DateTime.UtcNow);
        }

        logger.LogInformation("Sending jam state {HasJam} for station {Station}, requested {RequestTime}, last updated {LastUpdateTime}", jamState.HasPalletJam, jamState.Station, requestTime, jamState.LastStateChangeTime );

        await context.RespondAsync(new GotJamState(jamState.HasPalletJam));
    }
}