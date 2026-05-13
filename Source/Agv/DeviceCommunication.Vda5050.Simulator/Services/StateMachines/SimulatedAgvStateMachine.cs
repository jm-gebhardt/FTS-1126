using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.States;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.HeaderIds;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
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

    private IScheduleConfigurator<SimulatedAgv, ScheduleStatusUpdate> _statusUpdateScheduleConfiguration = null!;
    private IScheduleConfigurator<SimulatedAgv, SchedulePositionUpdate> _signalPositionScheduleConfiguration = null!;
    #endregion

    /// <summary>
    /// Interval for signalling position in [ms]
    /// </summary>
    public int SignalPositionIntervalMs { get; set; }

    /// <summary>
    /// Interval for updating status in [ms]
    /// </summary>
    public int StatusUpdateIntervalMs { get; set; }

    private readonly Options _options;
    private readonly ILogger<SimulatedAgvStateMachine> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IMapper _mapper;
    private readonly INurbsEdgeBuilder _edgeBuilder;

    /// <inheritdoc/>
    public SimulatedAgvStateMachine(ILogger<SimulatedAgvStateMachine> logger,
        Options options,
        TimeProvider timeProvider,
        ISequenceCounter<IStateHeaderId> stateHeaderId,
        ISequenceCounter<IVisualizationHeaderId> visualizationHeaderId,
        ISequenceCounter<IConnectionHeaderId> connectionHeaderId,
        IMapper mapper, INurbsEdgeBuilder edgeBuilder)
    {
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider;
        _mapper = mapper;
        _edgeBuilder = edgeBuilder;
        SignalPositionIntervalMs = options.VisualizationUpdateInterval;
        StatusUpdateIntervalMs = options.StateUpdateInterval;

        SetSagaFactory(stateHeaderId, visualizationHeaderId, connectionHeaderId);
        SetScheduler();
        InstanceState(x => x.CurrentState);

        Initially(SetInitialCreateAgvHandler());
        DuringAny(GraphUpdateHandler());
        DuringAny(UpdatePositionHandler());
        DuringAny(SetSignalPositionHandler());
        DuringAny(AbortOrderHandler());
        DuringAny(SetSendAgvStatusHandler());
        DuringAny(StartChargingHandler());
        DuringAny(StopChargingHandler());
        During(Idle, StartOrderHandler());
        DuringAny(UpdateOrderHandler());
        SetCompletedWhenFinalized(); // Delete when finalized
    }

    private void SetUpdateIntervals()
    {
        _statusUpdateScheduleConfiguration.Delay = TimeSpan.FromMilliseconds(StatusUpdateIntervalMs);
        _signalPositionScheduleConfiguration.Delay = TimeSpan.FromMilliseconds(SignalPositionIntervalMs);
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
                e.SetSagaFactory(context =>
                {
                    var saga = new SimulatedAgv(context.Message.CorrelationId,
                        context.Message.Configuration,
                        GetInitialBatteryCharge(),
                        _options.BatteryChargeRate,
                        _options.BatteryDischargeRate,
                        _timeProvider,
                        stateHeaderId,
                        visualizationHeaderId,
                        connectionHeaderId,
                        _edgeBuilder);
                    SetUpdateIntervals();
                    return saga;
                });
            }
        );
    }

    private void SetScheduler()
    {
        Schedule(() => StatusUpdateSchedule,
            s => s.SendStatusUpdateTokenId,
            configuration =>
            {
                _statusUpdateScheduleConfiguration = configuration;
                configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
            });

        Schedule(() => SignalPositionSchedule,
            s => s.SignalPositionTokenId,
            configuration =>
            {
                _signalPositionScheduleConfiguration = configuration;
                configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
            });
    }

    private int GetInitialBatteryCharge()
    {
        int maxDeviation = _options.InitialBatteryChargeDeviation;
        int calculatedBatteryCharge = _options.InitialBatteryCharge + Random.Shared.Next(-maxDeviation, maxDeviation + 1);
        const int safetyAgainstDeadBirth = 5;
        int initialBatteryCharge = Math.Clamp(calculatedBatteryCharge, SimulatedAgv.AliveChargeThreshold + safetyAgainstDeadBirth, (int)SimulatedAgv.MaxCharge);
        return initialBatteryCharge;
    }

    private EventActivityBinder<SimulatedAgv, CreateAgv> SetInitialCreateAgvHandler()
    {
        return When(CreateAgvEvent)
            .Then(c => _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(CreateAgv), c.Message.Configuration.SerialNumber, c.Saga.CorrelationId))
            .ThenAsync(PublishConnection)
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization)
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), GetStatusUpdateInterval)
            .Schedule(SignalPositionSchedule, c => new SchedulePositionUpdate(c.Saga.CorrelationId), GetPositionUpdateInterval)
            .TransitionTo(Idle);
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
            .If(c => c.Saga.CurrentOrder == null && c.Saga.CurrentState == nameof(BusyMoving), t => t.TransitionTo(Idle));
    }

    private TimeSpan GetStatusUpdateInterval(SagaConsumeContext<SimulatedAgv> context)
    {
        return TimeSpan.FromMilliseconds(StatusUpdateIntervalMs);
    }

    private TimeSpan GetPositionUpdateInterval(SagaConsumeContext<SimulatedAgv> context)
    {
        return TimeSpan.FromMilliseconds(SignalPositionIntervalMs);
    }

    private EventActivityBinder<SimulatedAgv, UpdatePosition> UpdatePositionHandler()
    {
        return When(UpdatePositionEvent)
            .Then(c =>
            {
                _logger.LogTrace("{MessageType} {AgvId} / {CorrelationId} received", nameof(CreateAgv), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.InitPosition(c.Message.Position, c.Message.LastNodeId);
            })
            .ThenAsync(PublishVisualization)
            .If(c => c.Saga.CurrentOrder?.State?.NewBaseRequest is true, t => t.ThenAsync(PublishState));
    }

    private EventActivityBinder<SimulatedAgv, StartOrder> StartOrderHandler()
    {
        return When(StartOrderEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(StartOrder), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.StartOrder(c.Message.Order);
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
            .ThenAsync(PublishVisualization);
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
                c.Saga.Charging = true;
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization);
    }

    private EventActivityBinder<SimulatedAgv, StopCharging> StopChargingHandler()
    {
        return When(StopChargingEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(StopCharging), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.Charging = false;
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization);
    }

    private EventActivityBinder<SimulatedAgv, UpdateOrder> UpdateOrderHandler()
    {
        return When(UpdateOrderEvent)
            .Then(c =>
            {
                _logger.LogInformation("{MessageType} {AgvId} / {CorrelationId} received", nameof(UpdateOrder), c.Saga.Configuration.SerialNumber, c.Saga.CorrelationId);
                c.Saga.UpdateOrder(c.Message.Order);
            })
            .ThenAsync(PublishState)
            .ThenAsync(PublishVisualization);
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
        context.Saga.StateHeaderId.Increase();
        context.Saga.UpdateStatus(GetStatusUpdateInterval(context).TotalSeconds);
        await context.PublishMqtt(new MqttState
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
            OperatingMode = MqttOperatingMode.Automatic,
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
            Loads = context.Saga.Loads.Select(l => new MqttLoad { LoadId = l.Id }).ToList(),
            ActionStates = context.Saga.CurrentOrder != null ? _mapper.Map<IList<MqttActionState>>(context.Saga.CurrentOrder!.PendingActionStates.ToList()) : [],
            BatteryState = new MqttBatteryState
            {
                BatteryCharge = context.Saga.SimulatedBatteryCharge,
                Charging = context.Saga.Charging,
                BatteryVoltage = 58
            },
            Errors = context.Saga.Error != null ? [_mapper.Map<MqttError>(context.Saga.Error)] : [],
            SafetyState = new MqttSafetyState
            {
                EStop = MqttEStop.None, FieldViolation = false
            }
        }, context.Saga.Configuration.GetRoutingKey(Vda5050Specifications.Subtopics.State), context.CancellationToken);
        context.Saga.CurrentOrder?.State?.NewBaseRequest = null;
        context.Saga.FinishOrder();
    }

    private async Task PublishVisualization(BehaviorContext<SimulatedAgv> context)
    {
        context.Saga.ProceedOrder();
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
        context.Saga.FinishOrder();
    }
}