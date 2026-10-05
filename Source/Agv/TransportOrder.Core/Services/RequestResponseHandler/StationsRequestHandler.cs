using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for station related requests.
/// </summary>
[UsedImplicitly]
public class StationsRequestHandler :
    IConsumer<GetMapStations>, 
    IConsumer<GetStations>, 
    IConsumer<GetStation>,
    IConsumer<TryLockStation>,
    IConsumer<TryUnlockStation>,
    IConsumer<UnblockStation>,
    IConsumer<SendJamStateRequest>
{
    private readonly ILogger<StationsRequestHandler> _logger;
    private readonly IMapRepository _mapRepository;
    private readonly IStationRepository _stationRepository;
    private readonly INodeRepository _nodeRepository;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IBus _bus;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Defines a handler for station related requests.
    /// </summary>
    public StationsRequestHandler(ILogger<StationsRequestHandler> logger,
        IMapRepository mapRepository,
        IStationRepository stationRepository,
        INodeRepository nodeRepository,
        IActionGroupRepository actionGroupRepository,
        IBus bus,
        ILoggingService loggingService)
    {
        _logger = logger;
        _mapRepository = mapRepository;
        _stationRepository = stationRepository;
        _nodeRepository = nodeRepository;
        _actionGroupRepository = actionGroupRepository;
        _bus = bus;
        _loggingService = loggingService;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetMapStations> context)
    {
        var map = await _mapRepository.GetMapAsync(context.Message.CorrelationId);
        var stations = await _stationRepository.GetStations(map.CorrelationId);
        await context.RespondAsync(new GotMapStations(stations));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetStations> context)
    {
        var cancellationToken = context.CancellationToken;
        var stations = await _stationRepository.GetStations(cancellationToken: cancellationToken);
        await context.RespondAsync(new GotStations(stations));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetStation> context)
    {
        var station = await _stationRepository.GetStation(context.Message.CorrelationId);
        await context.RespondAsync(new GotStation(station));
    }

    /// <summary>
    /// Handles the <see cref="TryLockStation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryLockStation> context)
    {
        (var id, string user, string reason, string lockDescription, var lockOrderHandling) = context.Message;
        var station = await _stationRepository.GetStation(id);
        if (station.IsLocked)
        {
            await context.RespondAsync(new LockedUnlockedStation(false));
            return;
        }
        await HandleOrders(station.Name, lockOrderHandling);
        await _bus.Publish(new LockStation(id, user, reason, lockDescription, lockOrderHandling));
        await _loggingService.LogStation(_logger, LogLevel.Information, LogCodes.StationOperatorInteraction, station.Id, "Operator interaction", "Operator interacted with station", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.StationLocked } });
        await context.RespondAsync(new LockedUnlockedStation(true));
    }

    private async Task HandleOrders(string stationName, LockedOrderHandlingType policy)
    {
        var actionGroups = await _actionGroupRepository.GetCurrentActionGroupsForStation(stationName);
        await CancelParkAndChargeActionGroups(actionGroups);
        await HandleStartedOrders(actionGroups, policy);
    }

    private async Task CancelParkAndChargeActionGroups(IReadOnlyCollection<ActionGroup> actionGroups)
    {
        var pendingParkAndChargeActionGroups = actionGroups.Where(a =>
            a.State == ActionGroupState.Created &&
            (a.Actions.Any(action => action.ActionType == ActionType.Charge) ||
             (a.Actions.Count == 1 && a.Actions.First().ActionType == ActionType.Move)));
        foreach (var actionGroup in pendingParkAndChargeActionGroups)
        {
            await _bus.Publish(new CancelActionGroup(actionGroup.CorrelationId, CancelReason.LockedStation));
        }
    }

    private async Task HandleStartedOrders(IReadOnlyCollection<ActionGroup> actionGroups, LockedOrderHandlingType policy)
    {
        var startedOrders = actionGroups.Where(a => a.State is ActionGroupState.Started or ActionGroupState.Paused);

        var handleOrder = GetHandlingForPolicy(policy);
        if (handleOrder == null)
        {
            return;
        }
        foreach (var actionGroup in startedOrders)
        {
            await handleOrder(actionGroup.CorrelationId);
        }
    }

    private Func<Guid, Task>? GetHandlingForPolicy(LockedOrderHandlingType policy)
    {
        if (policy == LockedOrderHandlingType.Abort)
        {
            return async correlationId => await _bus.Publish(new AbortActionGroup(correlationId, AbortReason.LockedStation));
        }
        if (policy == LockedOrderHandlingType.OnHold)
        {
            return async correlationId => await _bus.Publish(new PauseActionGroup(correlationId, PauseReason.LockedStation));
        }
        return null;
    }

    /// <summary>
    /// Handles the <see cref="TryUnlockStation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryUnlockStation> context)
    {
        (var id, string user) = context.Message;

        var station = await _stationRepository.GetStation(id);
        if (station.IsLocked)
        {
            await HandleOrdersWithPausedAction(station.Name, user);

            await _bus.Publish(new UnlockStation(id, user));
            await _loggingService.LogStation(_logger, LogLevel.Information, LogCodes.StationOperatorInteraction, station.Id, "Operator interaction", "Operator interacted with station", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.StationUnlocked } });
            await context.RespondAsync(new LockedUnlockedStation(true));
            return;
        }
        await context.RespondAsync(new LockedUnlockedStation(false));
    }

    private async Task HandleOrdersWithPausedAction(string stationName, string userEmail)
    {
        var actionGroups = await _actionGroupRepository.GetPausedActionGroupsForStation(stationName);

        foreach (var actionGroup in actionGroups)
        {
            await _bus.Publish(new ResumeActionGroup(actionGroup.CorrelationId, userEmail, FromStationUnlock: true));
        }
    }

    /// <summary>
    /// Handles the <see cref="UnblockStation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<UnblockStation> context)
    {
        var station = await _stationRepository.GetStation(context.Message.StationId);
        string user = context.Message.User;

        await _loggingService.LogStation(_logger, LogLevel.Information, LogCodes.StationUnblocked, station.Id, "Station Unblocked", "Station was unblocked", new Dictionary<string, object?>{{LogLabels.User, user}});
        await _loggingService.LogStation(_logger, LogLevel.Information, LogCodes.StationOperatorInteraction, station.Id, "Operator interaction", "Operator interacted with station", new Dictionary<string, object?>{{LogLabels.User, user}, {LogLabels.Operation, LogCodes.StationUnblocked}});

        await _bus.Publish(new Common.Messages.Stations.UnblockStation(station.Id));

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="SendJamStateRequest"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<SendJamStateRequest> context)
    {
        // check if node exists as station
        var station = await _stationRepository.GetStation(context.Message.CorrelationId);
        bool hasJamStateValue = station.AdditionalInfo.GetBool("HasJamState");
        
         _logger.LogInformation("Station {StationName} ({StationId}) supports JamState:  {HasJamState}", station.Name, station.Id, hasJamStateValue);
        if (hasJamStateValue)
        {
            var stationNode = await _nodeRepository.GetNodeAsync(context.Message.CorrelationId);
            string user = context.Message.User;

            await _loggingService.LogStation(_logger, LogLevel.Information, LogCodes.StationJamStateRequested, stationNode.Id, "JamState Requested", "JamState for station requested", new Dictionary<string, object?>{{LogLabels.User, user}});
            await _loggingService.LogStation(_logger, LogLevel.Information, LogCodes.StationOperatorInteraction, stationNode.Id, "Operator interaction", "Operator interacted with station", new Dictionary<string, object?>{{LogLabels.User, user}, {LogLabels.Operation, LogCodes.StationJamStateRequested}});

            await _bus.Publish(new Common.Messages.Graphs.Events.JamStateRequested(stationNode));
        }

        await context.RespondAsync(new Success());
    }
}