using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using JetBrains.Annotations;
using MassTransit;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for station related requests.
/// </summary>
[UsedImplicitly]
public class StationsRequestHandler(
    IMapRepository mapRepository,
    IStationRepository stationRepository,
    IActionGroupRepository actionGroupRepository,
    IBus bus) :
    IConsumer<GetMapStations>, 
    IConsumer<GetStations>, 
    IConsumer<GetStation>,
    IConsumer<TryLockStation>,
    IConsumer<TryUnlockStation>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetMapStations> context)
    {
        var map = await mapRepository.GetMapAsync(context.Message.CorrelationId);
        ICollection<Station> stations = await stationRepository.GetStations(map.MapId);
        await context.RespondAsync(new GotMapStations(stations));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetStations> context)
    {
        ICollection<Station> stations = await stationRepository.GetStations();
        await context.RespondAsync(new GotStations(stations));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetStation> context)
    {
        var station = await stationRepository.GetStation(context.Message.CorrelationId);
        await context.RespondAsync(new GotStation(station));
    }

    /// <summary>
    /// Handles the <see cref="TryLockStation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryLockStation> context)
    {
        (var id, string? user, string reason, string lockDescription, var lockOrderHandling) = context.Message;
        var station = await stationRepository.GetStation(id);
        if (station.IsLocked)
        {
            await context.RespondAsync(new LockedUnlockedStation(false));
            return;
        }
        await HandleOrders(station.Name, lockOrderHandling);
        await bus.Publish(new LockStation(id, user ,reason, lockDescription, lockOrderHandling));
        await context.RespondAsync(new LockedUnlockedStation(true));
    }

    private async Task HandleOrders(string stationName, LockedOrderHandlingType policy)
    {
        IReadOnlyCollection<ActionGroup> actionGroups = await actionGroupRepository.GetCurrentActionGroupsForStation(stationName);
        await CancelParkAndChargeActionGroups(actionGroups);
        await HandleStartedOrders(actionGroups, policy);
    }

    private async Task CancelParkAndChargeActionGroups(IReadOnlyCollection<ActionGroup> actionGroups)
    {
        IEnumerable<ActionGroup> pendingParkAndChargeActionGroups = actionGroups.Where(a =>
            a.State == ActionGroupState.Created &&
            (a.Actions.Any(action => action.ActionType == ActionType.Charge) ||
             (a.Actions.Count == 1 && a.Actions.First().ActionType == ActionType.Move)));
        foreach (var actionGroup in pendingParkAndChargeActionGroups)
        {
            await bus.Publish(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.LockedStation));
        }
    }

    private async Task HandleStartedOrders(IReadOnlyCollection<ActionGroup> actionGroups, LockedOrderHandlingType policy)
    {
        IEnumerable<ActionGroup> startedOrders = actionGroups.Where(a => a.State == ActionGroupState.Started);
 
        Func<Guid, Task>? handleOrder = GetHandlingForPolicy(policy);
        if (handleOrder == null)
        {
            return;
        }
        foreach (var actionGroup in startedOrders)
        {
            await handleOrder(actionGroup.CorrelationId) ;
        }
    }

    private Func<Guid, Task>? GetHandlingForPolicy(LockedOrderHandlingType policy)
    {
        if (policy == LockedOrderHandlingType.Abort)
        {
            return async (correlationId) => await bus.Publish(new AbortActionGroup(correlationId, AbortReason.LockedStation));
        }
        if (policy == LockedOrderHandlingType.OnHold)
        {
            return async (correlationId) => await bus.Publish(new PauseActionGroup(correlationId));
        }
        return null;
    }

    /// <summary>
    /// Handles the <see cref="TryUnlockStation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryUnlockStation> context)
    {
        var id = context.Message.CorrelationId;
        var station = await stationRepository.GetStation(id);
        if (station.IsLocked)
        {
            await bus.Publish(new UnlockStation(id));
            await context.RespondAsync(new LockedUnlockedStation(true));
            return;
        }
        await context.RespondAsync(new LockedUnlockedStation(false));
    }
}