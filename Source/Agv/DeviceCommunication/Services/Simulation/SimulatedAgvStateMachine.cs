using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.Common.Time;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Messages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;

/// <summary>
/// StateMachine to simulate an AGV
/// </summary>
[PublicAPI]
public class SimulatedAgvStateMachine : MassTransitStateMachine<SimulatedAgvSaga>
{
    #region Records
    /// <summary>
    /// Internal message (request) to create an AGV instance
    /// </summary>
    /// <param name="CorrelationId"></param>
    /// <param name="Agv"></param>
    /// <param name="NodeTravelTime"></param>
    public record CreateAgv(Guid CorrelationId, SimulatedAgv Agv, int NodeTravelTime) : CorrelatedBy<Guid>;

    /// <summary>
    /// Response to the CreateAgv request 
    /// </summary>
    /// <param name="CorrelationId"></param>
    public record CreateAgvResponse(Guid CorrelationId);

    /// <summary>
    /// Internal message to schedule sending status update
    /// </summary>
    /// <param name="CorrelationId"></param>
    public record ScheduleStatusUpdate(Guid CorrelationId) : CorrelatedBy<Guid>;

    /// <summary>
    /// Internal message to schedule finishing the action
    /// </summary>
    /// <param name="CorrelationId"></param>
    public record ScheduleFinishAction(Guid CorrelationId) : CorrelatedBy<Guid>;

    /// <summary>
    /// Internal message to schedule signaling of position
    /// </summary>
    /// <param name="CorrelationId"></param>
    public record SchedulePositionUpdate(Guid CorrelationId) : CorrelatedBy<Guid>;
    #endregion

    #region States
    /// <summary>
    /// AGV is idle
    /// </summary>
    public State Idle { get; private set; } = default!;

    /// <summary>
    /// AGV is busy and is moving
    /// </summary>
    public State BusyMoving { get; private set; } = default!;
    #endregion

    #region Events
    /// <summary>
    /// Action has been started
    /// </summary>
    public Event<StartActionCommand> StartActionCommandEvent { get; private set; } = default!;

    /// <summary>
    /// CreateAgv was received
    /// </summary>
    public Event<CreateAgv> CreateAgvEvent { get; private set; } = default!;

    /// <summary>
    /// RequestAgvGraph was received
    /// </summary>
    public Event<RequestAgvGraph> RequestAgvGraphEvent { get; private set; } = default!;

    /// <summary>
    /// RequestAgvData was received
    /// </summary>
    public Event<RequestAgvData> RequestAgvDataEvent { get; private set; } = default!;

    /// <summary>
    /// RequestAgvMap was received
    /// </summary>
    public Event<RequestAgvMap> RequestAgvMapEvent { get; private set; } = default!;

    /// <summary>
    /// RequestStatusSend was received.
    /// </summary>
    public Event<RequestStatusSendSchedulerStart> RequestStatusSendEvent { get; private set; } = default!;

    /// <summary>
    /// <see cref="ActionAborted"/> was received.
    /// </summary>
    public Event<ActionAborted> AgvActionAbortedEvent { get; private set; } = default!;

    /// <summary>
    /// SimulateAgvError was received
    /// </summary>
    public Event<SimulateAgvError> SimulateAgvErrorEvent { get; private set; } = default!;
    #endregion

    #region Schedules
    /// <summary>
    /// Status schedule triggered
    /// </summary>
    public Schedule<SimulatedAgvSaga, ScheduleStatusUpdate> StatusUpdateSchedule { get; private set; } = default!;

    /// <summary>
    /// Finish action schedule triggered
    /// </summary>
    public Schedule<SimulatedAgvSaga, ScheduleFinishAction> FinishActionSchedule { get; private set; } = default!;

    /// <summary>
    /// Trigger for scheduled signalling of position.
    /// </summary>
    public Schedule<SimulatedAgvSaga, SchedulePositionUpdate> SignalPositionSchedule { get; private set; } = default!;
    #endregion

    /// <summary>
    /// Interval for signalling position in [ms]
    /// </summary>
    public int SignalPositionIntervalMs { get; set; }

    /// <summary>
    /// Interval for updating status in [ms]
    /// </summary>
    public int StatusUpdateIntervalMs { get; set; }

    private readonly ILogger<SimulatedAgvStateMachine> _logger;
    private readonly SimulateOptions _simulateOptions;
    private readonly ITimeProvider _timeProvider;
    private readonly double _batteryDischargeRate; // Rate at which the battery charge decreases during simulation in [%/s]
    private readonly double _batteryChargeRate; // Rate at which the battery charge increases during charge in [%/s]
    private readonly int _initialBatteryCharge;

    /// <summary>
    /// SimulatedAgvStateMachine Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="simulateOptions"></param>
    /// <param name="timeProvider"></param>
    public SimulatedAgvStateMachine(ILogger<SimulatedAgvStateMachine> logger, SimulateOptions simulateOptions, ITimeProvider timeProvider)
    {
        _logger = logger;
        _simulateOptions = simulateOptions;
        _timeProvider = timeProvider;
        _batteryDischargeRate = _simulateOptions.BatteryDischargeRate;
        _batteryChargeRate = _simulateOptions.BatteryChargeRate;
        _initialBatteryCharge = _simulateOptions.InitialBatteryCharge;
        StatusUpdateIntervalMs = Math.Clamp(_simulateOptions.NodeTravelTime / 2, 100, 10000); // Send twice per node (min 0.1s, max 10s)
        SignalPositionIntervalMs = Math.Clamp(_simulateOptions.NodeTravelTime / 10, 200, 2000); // Send 10x per node (min 0.2s, max 2s)

        _logger.LogInformation("Initialized SimulatedAgvStateMachine with {GridRows} rows and node travel time of {NodeTravelTime} ms",
            _simulateOptions.GridRows, _simulateOptions.NodeTravelTime);

        SetSagaFactory();
        SetScheduler();
        InstanceState(x => x.CurrentState);

        Initially(SetInitialCreateAgvHandler());

        During(Idle, SetStartActionHandler());
        During(BusyMoving, Idle, SetFinishActionHandler());
        During(BusyMoving, ActionAbortedHandler());

        DuringAny(SetSendAgvStatusHandler());
        DuringAny(SetRequestAgvGraphHandler());
        DuringAny(SetRequestAgvDataHandler());
        DuringAny(SetRequestAgvMapHandler());
        DuringAny(SetSignalPositionHandler());
        DuringAny(SetRequestStatusSendHandler());
        DuringAny(SimulateAgvErrorHandler());

        WhenEnterAny(UpdateTimeOfStateTransition);

        SetCompletedWhenFinalized(); // Delete when finalized
    }

    #region Init
    private void SetScheduler()
    {
        Schedule(() => StatusUpdateSchedule,
            s => s.SendStatusUpdateTokenId,
            configuration =>
            {
                configuration.Delay = TimeSpan.FromMilliseconds(StatusUpdateIntervalMs);
                configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
            });

        Schedule(() => SignalPositionSchedule,
            s => s.SignalPositionTokenId,
            configuration =>
            {
                configuration.Delay = TimeSpan.FromMilliseconds(SignalPositionIntervalMs);
                configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
            });

        Schedule(() => FinishActionSchedule,
            s => s.FinishActionTokenId,
            configuration => { configuration.Received = r => r.CorrelateById(context => context.Message.CorrelationId); });
    }

    /// <summary>
    /// Set saga factory on <see cref="CreateAgv"/>
    /// </summary>
    private void SetSagaFactory()
    {
        Event(() => CreateAgvEvent, e =>
            e.SetSagaFactory(context => new SimulatedAgvSaga(context.Message.Agv, context.Message.NodeTravelTime, _initialBatteryCharge, _timeProvider))
        );
    }
    #endregion

    #region State transitions
    private EventActivityBinder<SimulatedAgvSaga, CreateAgv> SetInitialCreateAgvHandler()
    {
        return When(CreateAgvEvent)
            .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(CreateAgv), c.Message.Agv.AgvId, c.Saga.CorrelationId))
            .TransitionTo(Idle)
            .ThenAsync(PublishStatusAsync)
            .ThenAsync(PublishPositionAsync)
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), _ => TimeSpan.FromMilliseconds(StatusUpdateIntervalMs))
            .Schedule(SignalPositionSchedule, c => new SchedulePositionUpdate(c.Saga.CorrelationId), _ => TimeSpan.FromMilliseconds(SignalPositionIntervalMs))
            .Respond(c => new CreateAgvResponse(c.Saga.CorrelationId));
    }

    private EventActivityBinder<SimulatedAgvSaga, StartActionCommand> SetStartActionHandler()
    {
        return When(StartActionCommandEvent)
            .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(StartActionCommand), c.Message.AgvId, c.Saga.CorrelationId))
            .Then(Validate)
            .Then(c =>
            {
                SetCurrentAction(c);

                _logger.LogInformation("AGV {AgvId} / {CorrelationId} started action {ActionType} to {TargetNodeId} for {ParentId}. Action will be finished at {ActionFinishDate:HH:mm:ss.fff}",
                    c.Saga.AgvId, c.Saga.CorrelationId,
                    c.Message.Action.ActionType, c.Message.Action.TargetNodeId, c.Message.Action.ParentId, c.Saga.ActionFinishDate);
            })
            .TransitionTo(BusyMoving)
            .Publish(c => new ActionStarted(c.Saga.Action!.CorrelationId, c.Saga.CorrelationId))
            .ThenAsync(PublishStatusAsync)
            .Schedule(FinishActionSchedule,
                c => new ScheduleFinishAction(c.Saga.CorrelationId),
                c => c.Saga.ActionDuration!.Value)
            .Schedule(SignalPositionSchedule,
                c => new SchedulePositionUpdate(c.Saga.CorrelationId),
                _ => TimeSpan.FromMilliseconds(SignalPositionIntervalMs));
    }

    private EventActivityBinder<SimulatedAgvSaga, ActionAborted> ActionAbortedHandler()
    {
        return When(AgvActionAbortedEvent)
            .Then(c => _logger.LogDebug("Handling action {Action} abortion for agv  /{AgvCorrelationId}", c.Message.Action.CorrelationId, c.Message.CorrelationId))
            .ThenAsync(HandleActionAbortionAsync)
            .TransitionTo(Idle)
            .ThenAsync(PublishPositionAsync);
    }

    private static void SetCurrentAction(BehaviorContext<SimulatedAgvSaga, StartActionCommand> c)
    {
        c.Saga.LastActionAborted = false;
        c.Saga.Action = c.Message.Action;
        c.Saga.ActionStartDate = DateTime.UtcNow;
    }

    private EventActivityBinder<SimulatedAgvSaga, ScheduleFinishAction> SetFinishActionHandler()
    {
        return When(FinishActionSchedule.Received)
            .Then(c => _logger.LogDebug("{MessageType} {CorrelationId} received", nameof(ScheduleFinishAction), c.Saga.CorrelationId))
            .IfElse(c => c.Saga.LastActionAborted,
                e => e.Then(c => c.Saga.LastActionAborted = false),
                e => e.ThenAsync(FinishActionAsync)
                    .TransitionTo(Idle)
                    .ThenAsync(PublishStatusAsync)
                    .Then(c => _logger.LogDebug("{MessageType} {CorrelationId} received", nameof(ScheduleFinishAction), c.Saga.CorrelationId))
                    .ThenAsync(PublishPositionAsync));
    }
    #endregion

    #region Message handlers
    private EventActivityBinder<SimulatedAgvSaga, RequestStatusSendSchedulerStart> SetRequestStatusSendHandler()
    {
        return When(RequestStatusSendEvent)
            .Then(c => { _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(RequestStatusSendSchedulerStart), c.Saga.CorrelationId); })
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), _ => TimeSpan.FromMilliseconds(StatusUpdateIntervalMs));
    }

    private EventActivityBinder<SimulatedAgvSaga, ScheduleStatusUpdate> SetSendAgvStatusHandler()
    {
        return When(StatusUpdateSchedule.Received)
            .Then(c => { _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ScheduleStatusUpdate), c.Saga.CorrelationId); })
            .ThenAsync(PublishStatusAsync)
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), _ => TimeSpan.FromMilliseconds(StatusUpdateIntervalMs));
    }

    private EventActivityBinder<SimulatedAgvSaga, SchedulePositionUpdate> SetSignalPositionHandler()
    {
        return When(SignalPositionSchedule.Received)
            .Then(c => _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(SchedulePositionUpdate), c.Saga.CorrelationId))
            .ThenAsync(PublishPositionAsync)
            .Schedule(SignalPositionSchedule, c => new SchedulePositionUpdate(c.Saga.CorrelationId), _ => TimeSpan.FromMilliseconds(SignalPositionIntervalMs));
    }

    private EventActivityBinder<SimulatedAgvSaga, RequestAgvData> SetRequestAgvDataHandler()
    {
        return When(RequestAgvDataEvent)
            .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestAgvData), c.Message.AgvId, c.Saga.CorrelationId))
            .ThenAsync(PublishAgvData);
    }

    private EventActivityBinder<SimulatedAgvSaga, RequestAgvGraph> SetRequestAgvGraphHandler()
    {
        return When(RequestAgvGraphEvent)
            .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestAgvGraph), c.Message.AgvId, c.Saga.CorrelationId))
            .ThenAsync(PublishAgvGraph);
    }

    private EventActivityBinder<SimulatedAgvSaga, RequestAgvMap> SetRequestAgvMapHandler()
    {
        return When(RequestAgvMapEvent)
            .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} / {MapId} received", nameof(RequestAgvMap), c.Message.AgvId, c.Saga.CorrelationId, c.Message.MapId))
            .If(c => c.Saga.AgvMap != null,
                x => x.Publish(c =>
                {
                    var sagaAgvMap = c.Saga.AgvMap!;
                    return new AgvMapReceived(
                        c.Saga.CorrelationId,
                        c.Saga.AgvId,
                        new AgvMap(
                            c.Message.MapCorrelationId,
                            c.Message.MapId,
                            sagaAgvMap.ModificationDate,
                            sagaAgvMap.ImageData,
                            sagaAgvMap.Width,
                            sagaAgvMap.Height,
                            sagaAgvMap.Resolution,
                            sagaAgvMap.XOffset,
                            sagaAgvMap.YOffset
                        ));
                }));
    }

    private EventActivityBinder<SimulatedAgvSaga, SimulateAgvError> SimulateAgvErrorHandler()
    {
        return When(SimulateAgvErrorEvent)
            .Then(c => _logger.LogInformation("{MessageType} / {CorrelationId} received", nameof(SimulateAgvError), c.Saga.CorrelationId))
            .Then(c => { c.Saga.Errors = c.Message.AgvErrors; })
            .ThenAsync(PublishAgvError);
    }
    #endregion

    #region Actions
    private async Task PublishStatusAsync(BehaviorContext<SimulatedAgvSaga> context)
    {
        _logger.LogTrace("Status event from AGV {AgvId} / {CorrelationId}",
            context.Saga.AgvId, context.Saga.CorrelationId);

        bool prevCharging = context.Saga.AgvStatus.Charging;

        const double secondsPerMillisecond = 0.001;
        context.Saga.UpdateStatus(StatusUpdateIntervalMs * secondsPerMillisecond, _batteryChargeRate, _batteryDischargeRate); // Update status before each send

        if (context.Saga.IsOnline) // Only send status if AGV is still alive
        {
            await context.Publish(new AgvStatusReceived(
                context.Saga.CorrelationId,
                context.Saga.AgvId,
                context.Saga.AgvStatus
            ), context.CancellationToken);
        }

        // publish started/stopped charging regardless of IsOnline flag, because otherwise you can never reanimate dead Agvs
        // Started charging

        if (!prevCharging && context.Saga.AgvStatus.Charging && context.Saga.Action!.ActionType == ActionType.Charge)
        {
            _logger.LogInformation("StartCharging prevCharging=false, charging=true");
            await context.Publish(new AgvStartedCharging(context.Saga.CorrelationId));
        }
        // Stopped charging
        else if (prevCharging && !context.Saga.AgvStatus.Charging)
        {
            _logger.LogInformation("StopCharging prevCharging=true, charging=false");
            await context.Publish(new AgvStoppedCharging(context.Saga.CorrelationId));
        }
    }

    private async Task PublishPositionAsync(BehaviorContext<SimulatedAgvSaga> context)
    {
        var saga = context.Saga;
        string? previousNode = saga.LastNodeId;
        saga.UpdatePosition();
        var position = saga.Position;
        if (position != null)
        {
            _logger.LogTrace("Position event from AGV {AgvId} / {CorrelationId}: {Position}",
                saga.AgvId, saga.CorrelationId,
                position);

            await context.Publish(new AgvPositionReceived(
                saga.CorrelationId,
                saga.AgvId,
                position
            ), context.CancellationToken);

            if (previousNode != saga.LastNodeId)
            {
                // if node changed, publish status as well to avoid any delays
                await PublishStatusAsync(context);
            }
        }
    }

    private async Task PublishAgvData(BehaviorContext<SimulatedAgvSaga> context)
    {
        var agvSaga = context.Saga;
        if (agvSaga.AgvData != null)
        {
            await context.Publish(new AgvDataReceived(agvSaga.CorrelationId, agvSaga.AgvId, agvSaga.AgvData));
        }
    }

    private async Task PublishAgvGraph(BehaviorContext<SimulatedAgvSaga> context)
    {
        var agvSaga = context.Saga;
        if (agvSaga.AgvGraph != null)
        {
            await context.Publish(new AgvGraphReceived(agvSaga.CorrelationId, agvSaga.AgvId, agvSaga.AgvGraph));
        }
    }

    private static void Validate(BehaviorContext<SimulatedAgvSaga, StartActionCommand> context)
    {
        if (context.Message.Action == null)
        {
            throw new ArgumentException("Action data missing.");
        }
        if (!context.Message.Action.Nodes.Any())
        {
            throw new ArgumentException("Nodes missing.");
        }
    }

    private async Task FinishActionAsync(BehaviorContext<SimulatedAgvSaga, ScheduleFinishAction> context)
    {
        var action = context.Saga.Action;

        if (action == null)
        {
            _logger.LogError("Could not finish action, AgvAction object missing");
        }
        else
        {
            var agvCorrelationId = context.Saga.CorrelationId;
            _logger.LogInformation("AGV {AgvId} / {CorrelationId} finished action {ActionType} for {OrderId}",
                context.Saga.AgvId, agvCorrelationId, action.ActionType, action.ParentId);

            var cargos = new List<Cargo>();
            if (action.ActionType == ActionType.Pick)
            {
                // we just picked up a cargo
                var cargo = new Cargo(Guid.NewGuid(), agvCorrelationId, "Cargo 1", "default", new Point3D(0, 0, 0, 0), new Cuboid(100f, 200f, 50f), "top", 21);
                cargos.Add(cargo);
            }
            await context.Publish(new AgvLoadStatusReceived(agvCorrelationId, cargos));

            await context.Publish(new ActionCompleted(action.CorrelationId));
        }

        // update position last time before the ActionStart time is gone
        context.Saga.UpdatePosition();
        context.Saga.ResetAction();
    }

    private async Task HandleActionAbortionAsync(BehaviorContext<SimulatedAgvSaga, ActionAborted> context)
    {
        var action = context.Saga.Action;

        if (action == null)
        {
            _logger.LogError("Could not abort action, AgvAction object missing");
        }
        else
        {
            _logger.LogInformation("Agv {AgvId} / {CorrelationId} aborted action {ActionType} for {OrderId}",
                context.Saga.AgvId, context.Saga.CorrelationId, action.ActionType, action.ParentId);

            await context.Publish(new ActionAbortionCompleted(action.CorrelationId));
        }

        context.Saga.AbortAction();
    }

    private async Task PublishAgvError<T>(BehaviorContext<SimulatedAgvSaga, T> context) where T : class
    {
        _logger.LogDebug("Publish {Message} for Agv with Correlation Id: {AgvCorrelationId}", nameof(AgvErrorStatusReceived), context.Saga.CorrelationId);
        await context.Publish(new AgvErrorStatusReceived(context.Saga.CorrelationId, context.Saga.Errors), context.CancellationToken);
    }

    private EventActivityBinder<SimulatedAgvSaga> UpdateTimeOfStateTransition(EventActivityBinder<SimulatedAgvSaga> eventActivity)
    {
        return eventActivity.Then(ctx => ctx.Saga.TimeOfStateTransition = DateTime.UtcNow);
    }
    #endregion
}