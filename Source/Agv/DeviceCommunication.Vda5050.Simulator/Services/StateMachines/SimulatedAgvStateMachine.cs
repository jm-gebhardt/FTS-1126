using System.Text.Json;
using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.NewGraph.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.States;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.HeaderIds;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Serialization;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.StateMachines;

/// <summary>
/// StateMachine to simulate an agv.
/// </summary>
[PublicAPI]
public class SimulatedAgvStateMachine : MassTransitStateMachine<SimulatedAgv>
{
    #region States
    /// <summary>
    /// AGV is idle
    /// </summary>
    public State Idle { get; private set; } = null!;

    /// <summary>
    /// AGV is busy and is moving
    /// </summary>
    public State BusyMoving { get; private set; } = null!;
    #endregion

    /// <summary>
    /// <see cref="CreateAgv"/> received.
    /// </summary>
    public Event<CreateAgv> CreateAgvEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="UpdatePosition"/> received.
    /// </summary>
    public Event<UpdatePosition> UpdatePositionEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="StartOrder"/> received.
    /// </summary>
    public Event<StartOrder> StartOrderEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="UpdateOrder"/> received.
    /// </summary>
    public Event<UpdateOrder> UpdateOrderEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="AbortOrder"/> received.
    /// </summary>
    public Event<AbortOrder> AbortOrderEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="StartCharging"/> received.
    /// </summary>
    public Event<StartCharging> StartChargingEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="StopCharging"/> received.
    /// </summary>
    public Event<StopCharging> StopChargingEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="GraphUpdated"/> received.
    /// </summary>
    public Event<GraphUpdated> GraphUpdatedEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="SwitchOperatingMode"/> received.
    /// </summary>
    public Event<SwitchOperatingMode> OperatingModeEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="SetToPosition"/> received.
    /// </summary>
    public Event<SetToPosition> SetPositionEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="AddLoad"/> received.
    /// </summary>
    public Event<AddLoad> AddLoadEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="RemoveLoad"/> received.
    /// </summary>
    public Event<RemoveLoad> RemoveLoadEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="RemoveAgv"/> received.
    /// </summary>
    public Event<RemoveAgv> RemoveAgvEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="UpdateAgvConfiguration"/> received.
    /// </summary>
    public Event<UpdateAgvConfiguration> UpdateAgvConfigurationEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="RequestFactsheet"/> received.
    /// </summary>
    public Event<RequestFactsheet> RequestFactsheetEvent { get; private set; } = null!;

    #region Schedules
    /// <summary>
    /// Internal message to schedule sending status update
    /// </summary>
    /// <param name="CorrelationId"></param>
    public record ScheduleStatusUpdate(Guid CorrelationId) : CorrelatedBy<Guid>;

    /// <summary>
    /// Internal message to schedule signaling of position
    /// </summary>
    /// <param name="CorrelationId"></param>
    public record SchedulePositionUpdate(Guid CorrelationId) : CorrelatedBy<Guid>;

    /// <summary>
    /// State schedule triggered
    /// </summary>
    public Schedule<SimulatedAgv, ScheduleStatusUpdate> StatusUpdateSchedule { get; private set; } = null!;

    /// <summary>
    /// Trigger for scheduled signalling of position.
    /// </summary>
    public Schedule<SimulatedAgv, SchedulePositionUpdate> SignalPositionSchedule { get; private set; } = null!;
    #endregion

    private readonly ILogger<SimulatedAgvStateMachine> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IMapper _mapper;
    private readonly INurbsEdgeBuilder _edgeBuilder;
    private readonly IAgvStateCache _stateCache;
    private readonly IAgvOperatingModeCache _operatingModeCache;
    private readonly IFactsheetCache _factsheetCache;
    private readonly ISequenceCounter<IFactsheetHeaderId> _factsheetHeaderId;

    /// <inheritdoc/>
    public SimulatedAgvStateMachine(ILogger<SimulatedAgvStateMachine> logger,
        TimeProvider timeProvider,
        ISequenceCounter<IStateHeaderId> stateHeaderId,
        ISequenceCounter<IVisualizationHeaderId> visualizationHeaderId,
        ISequenceCounter<IConnectionHeaderId> connectionHeaderId,
        ISequenceCounter<IFactsheetHeaderId> factsheetHeaderId,
        IAgvOperatingModeCache operatingModeCache,
        IMapper mapper,
        INurbsEdgeBuilder edgeBuilder,
        IAgvStateCache stateCache,
        IFactsheetCache factsheetCache)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _mapper = mapper;
        _edgeBuilder = edgeBuilder;
        _stateCache = stateCache;
        _operatingModeCache = operatingModeCache;
        _factsheetCache = factsheetCache;
        _factsheetHeaderId = factsheetHeaderId;

        SetSagaFactory(stateHeaderId, visualizationHeaderId, connectionHeaderId);
        SetMissingInstanceHandling();
        SetScheduler();
        InstanceState(x => x.CurrentState);

        Initially(CreateAgvHandler());
        DuringAny(CreateAgvHandler());
        DuringAny(GraphUpdateHandler());
        DuringAny(UpdatePositionHandler());
        DuringAny(SetSignalPositionHandler());
        DuringAny(AbortOrderHandler());
        DuringAny(SetSendAgvStatusHandler());
        DuringAny(StartChargingHandler());
        DuringAny(StopChargingHandler());
        DuringAny(StartOrderHandler());
        DuringAny(UpdateOrderHandler());
        DuringAny(SetPositionHandler());
        DuringAny(SwitchOperatingModeHandler());
        DuringAny(AddLoadHandler());
        DuringAny(RemoveLoadHandler());
        DuringAny(RemoveAgvHandler());
        DuringAny(UpdateAgvConfigurationHandler());
        DuringAny(RequestFactsheetHandler());
        SetCompletedWhenFinalized(); // Delete when finalized
    }

    /// <summary>
    /// Set saga factory on <see cref="CreateAgv"/>
    /// </summary>
    private void SetSagaFactory(ISequenceCounter<IStateHeaderId> stateHeaderId,
        ISequenceCounter<IVisualizationHeaderId> visualizationHeaderId,
        ISequenceCounter<IConnectionHeaderId> connectionHeaderId)
    {
        Event(() => CreateAgvEvent, e =>
            {
                e.SetSagaFactory(context => new SimulatedAgv(context.Message.CorrelationId,
                    context.Message.Configuration,
                    GetInitialBatteryCharge(context.Message.Configuration),
                    context.Message.Configuration.BatteryChargeRate,
                    context.Message.Configuration.BatteryDischargeRate,
                    _timeProvider,
                    stateHeaderId,
                    visualizationHeaderId,
                    connectionHeaderId,
                    _edgeBuilder));
            }
        );
    }

    private void SetMissingInstanceHandling()
    {
        Event(() => RemoveAgvEvent, e => e.OnMissingInstance(m => m.Discard()));
        Event(() => UpdateAgvConfigurationEvent, e => e.OnMissingInstance(m => m.Discard()));
        Event(() => UpdatePositionEvent, e => e.OnMissingInstance(m => m.Discard()));
        Event(() => StartChargingEvent, e => e.OnMissingInstance(m => m.Discard()));
        Event(() => StopChargingEvent, e => e.OnMissingInstance(m => m.Discard()));
        Event(() => AbortOrderEvent, e => e.OnMissingInstance(m => m.Discard()));
        Event(() => GraphUpdatedEvent, e => e.OnMissingInstance(m => m.Discard()));
        Event(() => RequestFactsheetEvent, e => e.OnMissingInstance(m => m.Discard()));
    }

    private void SetScheduler()
    {
        Schedule(() => StatusUpdateSchedule,
            s => s.SendStatusUpdateTokenId,
            configuration =>
            {
                configuration.Delay = TimeSpan.FromMilliseconds(SimulatedAgvTypeConfiguration.DefaultStateUpdateIntervalMs);
                configuration.Received = r =>
                {
                    r.CorrelateById(context => context.Message.CorrelationId);
                    r.OnMissingInstance(m => m.Discard());
                };
            });

        Schedule(() => SignalPositionSchedule,
            s => s.SignalPositionTokenId,
            configuration =>
            {
                configuration.Delay = TimeSpan.FromMilliseconds(SimulatedAgvTypeConfiguration.DefaultVisualizationUpdateIntervalMs);
                configuration.Received = r =>
                {
                    r.CorrelateById(context => context.Message.CorrelationId);
                    r.OnMissingInstance(m => m.Discard());
                };
            });
    }

    private static int GetInitialBatteryCharge(AgvConfiguration configuration)
    {
        const int safetyAgainstDeadBirth = 5;
        return Math.Clamp(configuration.InitialBatteryCharge, SimulatedAgv.AliveChargeThreshold + safetyAgainstDeadBirth, (int)SimulatedAgv.MaxCharge);
    }

    private EventActivityBinder<SimulatedAgv, CreateAgv> CreateAgvHandler()
    {
        return When(CreateAgvEvent)
            .ThenAsync(async c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(CreateAgv), c.Message.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.ApplyConfiguration(c.Message.Configuration);
                c.Saga.GoOnline();
                if (c.Message.InitialNodes is { Count: > 0 } nodes)
                {
                    c.Saga.InitializePosition(nodes);
                }
                await _operatingModeCache.AddOrUpdateAsync(c.Saga.CorrelationId, c.Saga.OperatingMode);
            })
            .ThenAsync(PublishConnection)
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization)
            .Unschedule(StatusUpdateSchedule)
            .Unschedule(SignalPositionSchedule)
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), GetStatusUpdateInterval)
            .Schedule(SignalPositionSchedule, c => new SchedulePositionUpdate(c.Saga.CorrelationId), GetPositionUpdateInterval)
            .TransitionTo(Idle);
    }

    private EventActivityBinder<SimulatedAgv, UpdateAgvConfiguration> UpdateAgvConfigurationHandler()
    {
        return When(UpdateAgvConfigurationEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(UpdateAgvConfiguration), c.Message.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.ApplyConfiguration(c.Message.Configuration);
            })
            .Unschedule(StatusUpdateSchedule)
            .Unschedule(SignalPositionSchedule)
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), GetStatusUpdateInterval)
            .Schedule(SignalPositionSchedule, c => new SchedulePositionUpdate(c.Saga.CorrelationId), GetPositionUpdateInterval)
            .ThenAsync(PublishState);
    }

    private EventActivityBinder<SimulatedAgv, ScheduleStatusUpdate> SetSendAgvStatusHandler()
    {
        return When(StatusUpdateSchedule.Received)
            .Then(c => { _logger.LogTrace("{MessageType} {CorrelationId} received {State}", nameof(ScheduleStatusUpdate), c.Saga.CorrelationId, c.Saga.CurrentState); })
            .ThenAsync(PublishState)
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), GetStatusUpdateInterval)
            .If(c => c.Saga.CurrentOrder == null && c.Saga.CurrentState == nameof(BusyMoving), t => t.TransitionTo(Idle));
    }

    private EventActivityBinder<SimulatedAgv, SchedulePositionUpdate> SetSignalPositionHandler()
    {
        return When(SignalPositionSchedule.Received)
            .Then(c => _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(SchedulePositionUpdate), c.Saga.CorrelationId))
            .ThenAsync(PublishVisualization)
            .Schedule(SignalPositionSchedule, c => new SchedulePositionUpdate(c.Saga.CorrelationId), GetPositionUpdateInterval)
            .If(c => c.Saga.CurrentOrderReadyToFinish, t => t.ThenAsync(PublishState))
            .If(c => c.Saga.CurrentOrder == null && c.Saga.CurrentState == nameof(BusyMoving), t => t.TransitionTo(Idle));
    }

    private EventActivityBinder<SimulatedAgv, SwitchOperatingMode> SwitchOperatingModeHandler()
    {
        return When(OperatingModeEvent)
            .ThenAsync(async c =>
            {
                _logger.LogInformation("{MessageType} {CorrelationId} received", nameof(SwitchOperatingMode), c.Saga.CorrelationId);
                c.Saga.SwitchOperatingMode(c.Message.OperatingMode);
                await _operatingModeCache.AddOrUpdateAsync(c.Saga.CorrelationId, c.Saga.OperatingMode);
            })
            .If(c => !c.Saga.IsInAutomaticMode, t => t.TransitionTo(Idle))
            .ThenAsync(PublishState);
    }

    private EventActivityBinder<SimulatedAgv, RemoveAgv> RemoveAgvHandler()
    {
        return When(RemoveAgvEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(RemoveAgv), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.GoOffline();
            })
            .ThenAsync(PublishConnection)
            .Unschedule(StatusUpdateSchedule)
            .Unschedule(SignalPositionSchedule)
            .ThenAsync(async c =>
            {
                await _operatingModeCache.RemoveAsync(c.Saga.CorrelationId);
                await _stateCache.RemoveAsync(c.Saga.CorrelationId);
            })
            .Finalize();
    }

    private static TimeSpan GetStatusUpdateInterval(SagaConsumeContext<SimulatedAgv> context)
    {
        return TimeSpan.FromMilliseconds(context.Saga.Configuration.StateUpdateIntervalMs);
    }

    private static TimeSpan GetPositionUpdateInterval(SagaConsumeContext<SimulatedAgv> context)
    {
        return TimeSpan.FromMilliseconds(context.Saga.Configuration.VisualizationUpdateIntervalMs);
    }

    private EventActivityBinder<SimulatedAgv, UpdatePosition> UpdatePositionHandler()
    {
        return When(UpdatePositionEvent)
            .Then(c =>
            {
                _logger.LogTrace("{MessageType} {AgvId} / {CorrelationId} received", nameof(UpdatePosition), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.InitPosition(c.Message.Position, c.Message.LastNodeId);
            })
            .ThenAsync(PublishVisualization)
            .If(c => c.Saga.CurrentOrder?.State?.NewBaseRequest is true, t => t.ThenAsync(PublishState))
            .If(c => c.Saga.CurrentOrder == null && c.Saga.CurrentState == nameof(BusyMoving), t => t.TransitionTo(Idle));
    }

    private EventActivityBinder<SimulatedAgv, SetToPosition> SetPositionHandler()
    {
        return When(SetPositionEvent)
            .Then(c =>
            {
                _logger.LogTrace("{MessageType} {AgvId} / {CorrelationId} received", nameof(SetToPosition), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.SetPosition(c.Message.Position);
            })
            .ThenAsync(PublishVisualization)
            .ThenAsync(PublishState);
    }

    private EventActivityBinder<SimulatedAgv, AddLoad> AddLoadHandler()
    {
        return When(AddLoadEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(AddLoad), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.AddLoad(c.Message.LoadId, c.Message.LoadType, c.Message.LoadPosition);
            })
            .ThenAsync(PublishState);
    }

    private EventActivityBinder<SimulatedAgv, RemoveLoad> RemoveLoadHandler()
    {
        return When(RemoveLoadEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(RemoveLoad), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.RemoveLoad(c.Message.LoadId);
            })
            .ThenAsync(PublishState);
    }

    private EventActivityBinder<SimulatedAgv, StartOrder> StartOrderHandler()
    {
        return When(StartOrderEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(StartOrder), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.StartOrder(c.Message.Order, _logger);
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization)
            .TransitionTo(BusyMoving);
    }

    private EventActivityBinder<SimulatedAgv, GraphUpdated> GraphUpdateHandler()
    {
        return When(GraphUpdatedEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(GraphUpdated), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.InitializePosition(c.Message.Graph.Nodes);
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization)
            .If(c => c.Saga.CurrentOrder == null && c.Saga.CurrentState == nameof(BusyMoving), t => t.TransitionTo(Idle));
    }

    private EventActivityBinder<SimulatedAgv, AbortOrder> AbortOrderHandler()
    {
        return When(AbortOrderEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(AbortOrderEvent), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.AbortOrder(c.Message.ActionId);
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization)
            .TransitionTo(Idle);
    }

    private EventActivityBinder<SimulatedAgv, StartCharging> StartChargingHandler()
    {
        return When(StartChargingEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(StartCharging), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                if (c.Saga.IsInAutomaticMode)
                {
                    c.Saga.Charging = true;
                }
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization)
            .If(c => c.Saga.CurrentOrder == null && c.Saga.CurrentState == nameof(BusyMoving), t => t.TransitionTo(Idle));
    }

    private EventActivityBinder<SimulatedAgv, StopCharging> StopChargingHandler()
    {
        return When(StopChargingEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(StopCharging), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.StopCharging(_logger);
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization)
            .If(c => c.Saga.CurrentOrder == null && c.Saga.CurrentState == nameof(BusyMoving), t => t.TransitionTo(Idle));
    }

    private EventActivityBinder<SimulatedAgv, UpdateOrder> UpdateOrderHandler()
    {
        return When(UpdateOrderEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(UpdateOrder), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.UpdateOrder(c.Message.Order, _logger);
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization)
            .If(c => c.Saga.CurrentOrder == null && c.Saga.CurrentState == nameof(BusyMoving), t => t.TransitionTo(Idle));
    }

    private EventActivityBinder<SimulatedAgv, RequestFactsheet> RequestFactsheetHandler()
    {
        return When(RequestFactsheetEvent)
            .ThenAsync(async c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestFactsheet), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.RequestFactsheet(c.Message.ActionId);
                if (await _factsheetCache.TryGetAsync(c.Saga.Configuration.AgvTypeId) == null)
                {
                    await c.Publish(new FetchFactsheet(c.Saga.Configuration.AgvTypeId, c.Saga.Configuration.AgvTypeIdentifier), c.CancellationToken);
                }
            })
            .ThenAsync(PublishState);
    }

    private async Task<bool> PublishFactsheet(BehaviorContext<SimulatedAgv> context, string factsheetJson)
    {
        MqttFactsheet? factsheet;
        try
        {
            factsheet = JsonSerializer.Deserialize<MqttFactsheet>(factsheetJson, SystemTextJsonMessageSerializer.Options);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "The factsheet of agv type {AgvTypeIdentifier} is not valid JSON", context.Saga.Configuration.AgvTypeIdentifier);
            return false;
        }

        if (factsheet == null)
        {
            _logger.LogError("The factsheet of agv type {AgvTypeIdentifier} is empty", context.Saga.Configuration.AgvTypeIdentifier);
            return false;
        }

        _factsheetHeaderId.Increase();
        factsheet.HeaderId = (int)_factsheetHeaderId.Current;
        factsheet.Timestamp = _timeProvider.GetUtcNow();
        factsheet.Version = Vda5050Specifications.Version;
        factsheet.Manufacturer = context.Saga.Configuration.Manufacturer;
        factsheet.SerialNumber = context.Saga.Configuration.SerialNumber;
        await context.PublishMqtt(factsheet, context.Saga.Configuration.GetRoutingKey(Vda5050Specifications.Subtopics.Factsheet), context.CancellationToken);
        return true;
    }

    private async Task ResolveFactsheetRequests(BehaviorContext<SimulatedAgv> context)
    {
        string? factsheetJson = await _factsheetCache.TryGetAsync(context.Saga.Configuration.AgvTypeId);
        context.Saga.SetFactsheetAvailable(factsheetJson != null);
        if (factsheetJson != null && context.Saga.HasWaitingFactsheetRequests && await PublishFactsheet(context, factsheetJson))
        {
            context.Saga.FinishFactsheetRequests();
        }
    }

    private async Task PublishConnection(BehaviorContext<SimulatedAgv> context)
    {
        context.Saga.ConnectionHeaderId.Increase();

        await context.PublishMqtt(new MqttConnection
        {
            HeaderId = context.Saga.ConnectionHeaderId.Current,
            ConnectionState = context.Saga.IsOnline ? MqttConnectionState.ONLINE : MqttConnectionState.OFFLINE,
            Manufacturer = context.Saga.Configuration.Manufacturer,
            SerialNumber = context.Saga.Configuration.SerialNumber,
            Version = Vda5050Specifications.Version,
            Timestamp = _timeProvider.GetUtcNow()
        }, context.Saga.Configuration.GetRoutingKey(Vda5050Specifications.Subtopics.Connection), context.CancellationToken);
    }

    private async Task PublishState(BehaviorContext<SimulatedAgv> context)
    {
        await ResolveFactsheetRequests(context);
        context.Saga.StateHeaderId.Increase();
        context.Saga.UpdateStatus(GetStatusUpdateInterval(context).TotalSeconds, _logger);
        string actionStatesSummary = context.Saga.CurrentOrder == null ? "(no order)" :
            context.Saga.CurrentOrder.PendingActionStates.Count == 0 ? "(empty)" : string.Join(",", context.Saga.CurrentOrder.PendingActionStates.Select(a => $"{a.ActionType}#{a.ActionId[..Math.Min(8, a.ActionId.Length)]}={a.ActionStatus}"));
        _logger.LogInformation("PublishState {Agv}: orderId={OrderId} updateId={OrderUpdateId} lastNodeId={LastNodeId} actionStates=[{ActionStates}] loads={LoadCount}",
            context.Saga.Configuration.SerialNumber,
            context.Saga.CurrentOrder?.OrderId ?? "(none)",
            context.Saga.CurrentOrder?.OrderUpdateId ?? 0,
            context.Saga.LastNodeId ?? "(null)",
            actionStatesSummary,
            context.Saga.Loads.Count);
        var state = new MqttState
        {
            HeaderId = context.Saga.StateHeaderId.Current,
            Timestamp = _timeProvider.GetUtcNow(),
            Version = Vda5050Specifications.Version,
            Manufacturer = context.Saga.Configuration.Manufacturer,
            SerialNumber = context.Saga.Configuration.SerialNumber,
            Maps = null,
            OrderId = context.Saga.CurrentOrder?.OrderId ?? string.Empty,
            OrderUpdateId = context.Saga.CurrentOrder?.OrderUpdateId ?? 0,
            LastNodeId = context.Saga.LastNodeId,
            LastNodeSequenceId = context.Saga.LastNodeSequenceId,
            NewBaseRequest = context.Saga.CurrentOrder?.State?.NewBaseRequest,
            Driving = false,
            OperatingMode = _mapper.Map<MqttOperatingMode>(context.Saga.OperatingMode),
            NodeStates = context.Saga.CurrentOrder?.CurrentRoute.Nodes.Select(n => new MqttNodeState { NodeDescription = n.NodeDescription, NodeId = n.NodeId, Released = n.Released }).ToList() ?? [],
            EdgeStates = context.Saga.CurrentOrder?.CurrentRoute.Edges.Select(e => new MqttEdgeState { EdgeDescription = e.EdgeDescription, EdgeId = e.EdgeId, Released = e.Released }).ToList() ?? [],
            AgvPosition = new MqttAgvPosition
            {
                DeviationRange = context.Saga.CurrentPosition.DeviationRange,
                MapId = context.Saga.Configuration.MapId,
                PositionInitialized = context.Saga.CurrentPosition.PositionInitialized,
                Theta = context.Saga.CurrentPosition.Theta,
                X = context.Saga.CurrentPosition.X,
                Y = context.Saga.CurrentPosition.Y
            },
            Loads = context.Saga.Loads.Select(l => new MqttLoad { LoadId = l.Id, LoadType = l.LoadType, LoadPosition = l.LoadPosition }).ToList(),
            ActionStates = _mapper.Map<IList<MqttActionState>>((context.Saga.CurrentOrder?.PendingActionStates ?? []).Concat(context.Saga.InstantActionStates).ToList()),
            BatteryState = new MqttBatteryState
            {
                BatteryCharge = context.Saga.SimulatedBatteryCharge,
                Charging = context.Saga.Charging,
                BatteryVoltage = 58
            },
            Errors = _mapper.Map<IList<MqttError>>(context.Saga.Errors.ToList()),
            SafetyState = new MqttSafetyState
            {
                EStop = MqttEStop.None, FieldViolation = false
            }
        };
        await context.PublishMqtt(state, context.Saga.Configuration.GetRoutingKey(Vda5050Specifications.Subtopics.State), context.CancellationToken);
        await _stateCache.AddOrUpdateAsync(context.Saga.CorrelationId, state);
        await context.Publish(new SimulatedAgvStateChanged(context.Saga.CorrelationId, state), context.CancellationToken);
        context.Saga.RemoveFinishedInstantActionStates();
        context.Saga.CurrentOrder?.State?.NewBaseRequest = null;
        context.Saga.FinishOrder(_logger);
    }

    private async Task PublishVisualization(BehaviorContext<SimulatedAgv> context)
    {
        context.Saga.ProceedOrder(_logger);
        context.Saga.VisualizationHeaderId.Increase();
        var message = new MqttVisualization
        {
            HeaderId = context.Saga.VisualizationHeaderId.Current,
            Timestamp = _timeProvider.GetUtcNow(),
            Version = Vda5050Specifications.Version,
            Manufacturer = context.Saga.Configuration.Manufacturer,
            SerialNumber = context.Saga.Configuration.SerialNumber,
            AgvPosition = new MqttAgvPosition
            {
                DeviationRange = context.Saga.CurrentPosition.DeviationRange,
                MapId = context.Saga.Configuration.MapId,
                PositionInitialized = context.Saga.CurrentPosition.PositionInitialized,
                Theta = context.Saga.CurrentPosition.Theta,
                X = context.Saga.CurrentPosition.X,
                Y = context.Saga.CurrentPosition.Y
            }
        };
        await context.PublishMqtt(message, context.Saga.Configuration.GetRoutingKey(Vda5050Specifications.Subtopics.Visualization), context.CancellationToken);
    }
}