using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Commands;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Speed;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;
using Gebhardt.Acc.DeviceCommunication.SimAgv.States;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;

/// <summary>
/// StateMachine to simulate an agv.
/// </summary>
[PublicAPI]
public class SimulatedAgvStateMachine : MassTransitStateMachine<SimulatedAgvSaga>
{
    private const int InitialTravelTime = 5000;

    #region Records
    /// <summary>
    /// Internal message (request) to create an agv instance
    /// </summary>
    /// <param name="CorrelationId"></param>
    /// <param name="Agv"></param>
    /// <param name="SimulationSpeed"></param>
    public record CreateAgv(Guid CorrelationId, SimulatedAgv Agv, int SimulationSpeed) : CorrelatedBy<Guid>;

    /// <summary>
    /// Response to the CreateAgv request.
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
    /// <param name="CorrelationId">The correlation Id of the current action.</param>
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
    public State Idle { get; private set; } = null!;

    /// <summary>
    /// AGV is busy and is moving
    /// </summary>
    public State BusyMoving { get; private set; } = null!;
    #endregion

    #region Events
    /// <summary>
    /// Action has been started
    /// </summary>
    public Event<DispatchRouteCommand> StartActionCommandEvent { get; private set; } = null!;

    /// <summary>
    /// Action has been updated
    /// </summary>
    public Event<UpdateDispatchedRouteCommand> UpdateActionCommandEvent { get; private set; } = null!;

    /// <summary>
    /// Abort and start action.
    /// </summary>
    public Event<AbortAndDispatchRouteCommand> AbortAndStartActionCommandEvent { get; private set; } = null!;

    /// <summary>
    /// CreateAgv was received
    /// </summary>
    public Event<CreateAgv> CreateAgvEvent { get; private set; } = null!;

    /// <summary>
    /// RequestAgvGraph was received
    /// </summary>
    public Event<RequestAgvGraph> RequestAgvGraphEvent { get; private set; } = null!;

    /// <summary>
    /// RequestAgvData was received
    /// </summary>
    public Event<RequestAgvData> RequestAgvDataEvent { get; private set; } = null!;

    /// <summary>
    /// RequestAgvZones was received
    /// </summary>
    public Event<RequestAgvZones> RequestAgvZonesEvent { get; private set; } = null!;

    /// <summary>
    /// RemoveAgvFromZone was received
    /// </summary>
    public Event<RemoveAgvFromZone> RemoveAgvFromZoneEvent { get; private set; } = null!;

    /// <summary>
    /// <see cref="ActionAborted"/> was received.
    /// </summary>
    public Event<ActionAborted> AgvActionAbortedEvent { get; private set; } = null!;

    /// <summary>
    /// SimulateAgvError was received
    /// </summary>
    public Event<SimulateAgvError> SimulateAgvErrorEvent { get; private set; } = null!;

    /// <summary>
    /// SimulateActionFailure was received
    /// </summary>
    public Event<SimulateActionFailure> SimulateActionFailureEvent { get; private set; } = null!;

    /// <summary>
    /// ResetAgvErrors was received
    /// </summary>
    public Event<ResetAgvErrors> ResetAgvErrorsEvent { get; private set; } = null!;

    /// <summary>
    /// Simulate an immediate charge up to given level.
    /// </summary>
    public Event<SimulateCharging> SimulateChargingEvent { get; private set; } = null!;

    /// <summary>
    /// Event to adjust the simulation speed of an AGV.
    /// </summary>
    public Event<SetSimulationSpeedForAgv> SetSimulationSpeedEvent { get; private set; } = null!;
    #endregion

    #region Schedules
    /// <summary>
    /// Status schedule triggered
    /// </summary>
    public Schedule<SimulatedAgvSaga, ScheduleStatusUpdate> StatusUpdateSchedule { get; private set; } = null!;

    /// <summary>
    /// Finish action schedule triggered
    /// </summary>
    public Schedule<SimulatedAgvSaga, ScheduleFinishAction> FinishActionSchedule { get; private set; } = null!;

    /// <summary>
    /// Trigger for scheduled signalling of position.
    /// </summary>
    public Schedule<SimulatedAgvSaga, SchedulePositionUpdate> SignalPositionSchedule { get; private set; } = null!;
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
    private readonly Options _options;
    private readonly TimeProvider _timeProvider;
    private IScheduleConfigurator<SimulatedAgvSaga, ScheduleStatusUpdate> _statusUpdateScheduleConfiguration = null!;
    private IScheduleConfigurator<SimulatedAgvSaga, SchedulePositionUpdate> _signalPositionScheduleConfiguration = null!;

    private double BatteryDischargeRate => _options.BatteryDischargeRate; // Rate at which the battery charge decreases during simulation in [%/s]

    private double BatteryChargeRate => _options.BatteryChargeRate; // Rate at which the battery charge increases during charge in [%/s]

    private string[]? SpawnAt => _options.SpawnAt?.ToArray(); // Id of nodes where AGVs will spawn at simulation start up

    /// <summary>
    /// SimulatedAgvStateMachine Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    /// <param name="timeProvider"></param>
    public SimulatedAgvStateMachine(ILogger<SimulatedAgvStateMachine> logger, Options options, TimeProvider timeProvider)
    {
        _logger = logger;
        _options = options;
        _timeProvider = timeProvider;

        _logger.LogInformation("Initialized SimulatedAgvStateMachine with Graph (fixed :{FixedGraph}/ grid:{GridGraph} with rows {Rows})", _options.FixedGraph, _options.GridGraph, _options.GridRows);

        SetSagaFactory();
        SetScheduler();
        InstanceState(x => x.CurrentState);

        Initially(SetInitialCreateAgvHandler());

        During(Idle, SetStartActionHandler());
        During(Idle, SetUpdateActionHandler());
        During(BusyMoving, Idle, SetFinishActionHandler());
        During(BusyMoving, Idle, ActionAbortedHandler());
        During(BusyMoving, SetUpdateActionHandler());

        DuringAny(SetAbortAndStartActionHandler());
        DuringAny(SetSendAgvStatusHandler());
        DuringAny(SetRequestAgvGraphHandler());
        DuringAny(SetRequestAgvDataHandler());
        DuringAny(SetRequestAgvZonesHandler());
        DuringAny(SetSignalPositionHandler());
        DuringAny(SimulateAgvErrorHandler());
        DuringAny(ResetAgvErrorsHandler());
        DuringAny(RemoveAgvFromZoneHandler());
        DuringAny(SimulateChargingHandler());
        DuringAny(SetSimulationSpeedHandler());
        DuringAny(SimulateActionFailureHandler());

        WhenEnterAny(UpdateTimeOfStateTransition);

        SetCompletedWhenFinalized(); // Delete when finalized
    }

    private void SetUpdateIntervals(int simulationSpeed)
    {
        StatusUpdateIntervalMs = Math.Clamp(simulationSpeed * InitialTravelTime / 2, 100, 10000); // Send twice per node (min 0.1s, max 10s)
        SignalPositionIntervalMs = Math.Clamp(simulationSpeed * InitialTravelTime / 10, 200, 2000); // Send 10x per node (min 0.2s, max 2s)

        _statusUpdateScheduleConfiguration.Delay = TimeSpan.FromMilliseconds(StatusUpdateIntervalMs);
        _signalPositionScheduleConfiguration.Delay = TimeSpan.FromMilliseconds(SignalPositionIntervalMs);
    }

    #region Init
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
            {
                e.SetSagaFactory(context =>
                {
                    var saga = new SimulatedAgvSaga(context.Message.Agv, context.Message.SimulationSpeed, GetInitialBatteryCharge(), BatteryChargeRate, _timeProvider, null, SpawnAt);
                    SetUpdateIntervals(saga.SimulationSpeed);
                    return saga;
                });
            }
        );
    }

    private int GetInitialBatteryCharge()
    {
        int maxDeviation = _options.InitialBatteryChargeDeviation;
        int calculatedBatteryCharge = _options.InitialBatteryCharge + Random.Shared.Next(-maxDeviation, maxDeviation + 1);
        const int safetyAgainstDeadBirth = 5;
        int initialBatteryCharge = Math.Clamp(calculatedBatteryCharge, SimulatedAgvSaga.AliveChargeThreshold + safetyAgainstDeadBirth, (int)SimulatedAgvSaga.MaxCharge);
        return initialBatteryCharge;
    }
    #endregion

    #region State transitions
    private EventActivityBinder<SimulatedAgvSaga, CreateAgv> SetInitialCreateAgvHandler()
    {
        return When(CreateAgvEvent)
            .Then(c => { _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(CreateAgv), c.Message.Agv.SerialNumber, c.Saga.CorrelationId); })
            .TransitionTo(Idle)
            .ThenAsync(PublishStatusAsync)
            .ThenAsync(PublishPositionAsync)
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), GetStatusUpdateInterval)
            .Schedule(SignalPositionSchedule, c => new SchedulePositionUpdate(c.Saga.CorrelationId), GetPositionScheduleInterval)
            .Respond(c => new CreateAgvResponse(c.Saga.CorrelationId));
    }

    private EventActivityBinder<SimulatedAgvSaga, DispatchRouteCommand> SetStartActionHandler()
    {
        return When(StartActionCommandEvent)
            .Then(c => _logger.LogDebug("{MessageType} SerialNumber {AgvId} / CorrelationId {CorrelationId} / {ActionType} Action CorrelationId {ActionCorrelationId} received", nameof(DispatchRouteCommand), c.Message.SerialNumber, c.Message.CorrelationId, c.Message.Action.ActionType, c.Message.Action.CorrelationId))
            .Then(Validate)
            .Then(c =>
            {
                SetCurrentAction(c);
                if (c.Message.Action.IsMoveToCoordinate)
                {
                    _logger.LogInformation("AGV {AgvId} / {CorrelationId} started action {ActionType} to ({Position}) for {ParentId}. Action will be finished at {ActionFinishDate:HH:mm:ss.fff}",
                        c.Saga.SerialNumber, c.Saga.CorrelationId,
                        c.Message.Action.ActionType, c.Message.Action.Target.Position, c.Message.Action.ParentId, c.Saga.ActionFinishDate);
                }
                else
                {
                    _logger.LogInformation("AGV {AgvId} / {CorrelationId} started action {ActionType} to {TargetNodeId} for {ParentId}. Action will be finished at {ActionFinishDate:HH:mm:ss.fff}",
                        c.Saga.SerialNumber, c.Saga.CorrelationId,
                        c.Message.Action.ActionType, c.Message.Action.Target.Node!.Name, c.Message.Action.ParentId, c.Saga.ActionFinishDate);
                }
            })
            .TransitionTo(BusyMoving)
            .Publish(c => new ActionStartedByAgv(c.Saga.Action!.CorrelationId, c.Saga.CorrelationId))
            .ThenAsync(PublishStatusAsync)
            .Schedule(FinishActionSchedule,
                c => new ScheduleFinishAction(c.Saga.CorrelationId),
                c => c.Saga.ActionDuration!.Value)
            .Schedule(SignalPositionSchedule,
                c => new SchedulePositionUpdate(c.Saga.CorrelationId),
                GetPositionScheduleInterval);
    }

    private EventActivityBinder<SimulatedAgvSaga, UpdateDispatchedRouteCommand> SetUpdateActionHandler()
    {
        return When(UpdateActionCommandEvent)
            .Then(c => _logger.LogDebug("{MessageType} SerialNumber {AgvId} / CorrelationId {CorrelationId} / {ActionType} Action CorrelationId {ActionCorrelationId} received", nameof(UpdateDispatchedRouteCommand), c.Message.SerialNumber, c.Message.CorrelationId, c.Message.Action.ActionType, c.Message.Action.CorrelationId))
            .Then(Validate)
            .Then(c =>
            {
                SetCurrentAction(c);
                if (c.Message.Action.IsMoveToCoordinate)
                {
                    _logger.LogInformation("AGV {AgvId} / {CorrelationId} updated action {ActionType} ({ActionCorrelationId}) to ({Position}) for {ParentId}. Action will be finished at {ActionFinishDate:HH:mm:ss.fff}",
                        c.Saga.SerialNumber, c.Saga.CorrelationId,
                        c.Message.Action.ActionType, c.Message.Action.CorrelationId, c.Message.Action.Target.Position, c.Message.Action.ParentId, c.Saga.ActionFinishDate);
                }
                else
                {
                    _logger.LogInformation("AGV {AgvId} / {CorrelationId} updated action {ActionType} ({ActionCorrelationId}) to {TargetNodeId} for {ParentId}. Action will be finished at {ActionFinishDate:HH:mm:ss.fff}",
                        c.Saga.SerialNumber, c.Saga.CorrelationId,
                        c.Message.Action.ActionType, c.Message.Action.CorrelationId, c.Message.Action.Target.Node!.Name, c.Message.Action.ParentId, c.Saga.ActionFinishDate);
                }
            })
            .Publish(c => new ActionUpdatedByAgv(c.Saga.Action!.CorrelationId, c.Saga.CorrelationId))
            .ThenAsync(PublishStatusAsync)
            .Schedule(FinishActionSchedule,
                c => new ScheduleFinishAction(c.Saga.CorrelationId),
                c => c.Saga.ActionDuration!.Value)
            .Schedule(SignalPositionSchedule,
                c => new SchedulePositionUpdate(c.Saga.CorrelationId),
                GetPositionScheduleInterval);
    }

    private EventActivityBinder<SimulatedAgvSaga, AbortAndDispatchRouteCommand> SetAbortAndStartActionHandler()
    {
        return When(AbortAndStartActionCommandEvent)
            .Then(c => _logger.LogDebug("{MessageType} SerialNumber {AgvId} / CorrelationId {CorrelationId} / {ActionType} Action CorrelationId {ActionCorrelationId} received", nameof(AbortAndDispatchRouteCommand), c.Message.SerialNumber, c.Message.CorrelationId, c.Message.Action.ActionType, c.Message.Action.CorrelationId))
            .Then(Validate)
            .Then(c =>
            {
                SetCurrentAction(c);
                if (c.Message.Action.IsMoveToCoordinate)
                {
                    _logger.LogInformation("AGV {AgvId} / {CorrelationId} updated action {ActionType} ({ActionCorrelationId}) to ({Position}) for {ParentId}. Action will be finished at {ActionFinishDate:HH:mm:ss.fff}",
                        c.Saga.SerialNumber, c.Saga.CorrelationId,
                        c.Message.Action.ActionType, c.Message.Action.CorrelationId, c.Message.Action.Target.Position, c.Message.Action.ParentId, c.Saga.ActionFinishDate);
                }
                else
                {
                    _logger.LogInformation("AGV {AgvId} / {CorrelationId} updated action {ActionType} ({ActionCorrelationId}) to {TargetNodeId} for {ParentId}. Action will be finished at {ActionFinishDate:HH:mm:ss.fff}",
                        c.Saga.SerialNumber, c.Saga.CorrelationId,
                        c.Message.Action.ActionType, c.Message.Action.CorrelationId, c.Message.Action.Target.Node!.Name, c.Message.Action.ParentId, c.Saga.ActionFinishDate);
                }
            })
            .Publish(c => new ActionUpdatedByAgv(c.Saga.Action!.CorrelationId, c.Saga.CorrelationId))
            .ThenAsync(PublishStatusAsync)
            .Schedule(FinishActionSchedule,
                c => new ScheduleFinishAction(c.Saga.CorrelationId),
                c => c.Saga.ActionDuration!.Value)
            .Schedule(SignalPositionSchedule,
                c => new SchedulePositionUpdate(c.Saga.CorrelationId),
                GetPositionScheduleInterval);
    }

    private EventActivityBinder<SimulatedAgvSaga, ActionAborted> ActionAbortedHandler()
    {
        return When(AgvActionAbortedEvent)
            .Then(c => _logger.LogDebug("Handling action {Action} abortion for agv  /{AgvCorrelationId}", c.Message.Action.CorrelationId, c.Message.CorrelationId))
            .ThenAsync(HandleActionAbortionAsync)
            .TransitionTo(Idle)
            .ThenAsync(PublishPositionAsync);
    }

    private void SetCurrentAction(BehaviorContext<SimulatedAgvSaga, DispatchRouteCommand> c)
    {
        c.Saga.LastActionAborted = false;
        c.Saga.Action = c.Message.Action;
        c.Saga.ActionStartDate = _timeProvider.GetUtcNow();
        if (c.Message.Action.IsMoveToCoordinate)
        {
            c.Saga.TravelSimulator.EstimateTravelTimes(c.Message.Action.Target.Position!, c.Saga.ActionStartDate!.Value, c.Saga.AgvData!, 0);
            return;
        }
        c.Saga.TravelSimulator.EstimateTravelTimes(c.Message.Route.BaseNodes.Select(n => n.Name).ToArray(), c.Message.Route.BaseEdges.Select(e => e.Id).ToArray(), c.Saga.ActionStartDate!.Value, c.Saga.AgvData!, 0);
    }

    private void SetCurrentAction(BehaviorContext<SimulatedAgvSaga, UpdateDispatchedRouteCommand> c)
    {
        c.Saga.LastActionAborted = false;
        c.Saga.Action = c.Message.Action;
        c.Saga.ActionStartDate = _timeProvider.GetUtcNow();
        if (c.Message.Action.IsMoveToCoordinate)
        {
            c.Saga.TravelSimulator.EstimateTravelTimes(c.Message.Action.Target.Position!, c.Saga.ActionStartDate!.Value, c.Saga.AgvData!, 0);
            return;
        }
        c.Saga.TravelSimulator.EstimateTravelTimes(c.Message.Route.BaseNodes.Select(n => n.Name).ToArray(), c.Message.Route.BaseEdges.Select(e => e.Id).ToArray(), c.Saga.ActionStartDate!.Value, c.Saga.AgvData!, 0);
    }

    private void SetCurrentAction(BehaviorContext<SimulatedAgvSaga, AbortAndDispatchRouteCommand> c)
    {
        c.Saga.LastActionAborted = false;
        c.Saga.Action = c.Message.Action;
        c.Saga.ActionStartDate = _timeProvider.GetUtcNow();
        if (c.Message.Action.IsMoveToCoordinate)
        {
            c.Saga.TravelSimulator.EstimateTravelTimes(c.Message.Action.Target.Position!, c.Saga.ActionStartDate!.Value, c.Saga.AgvData!, 0);
            return;
        }
        c.Saga.TravelSimulator.EstimateTravelTimes(c.Message.Route.BaseNodes.Select(n => n.Name).ToArray(), c.Message.Route.BaseEdges.Select(e => e.Id).ToArray(), c.Saga.ActionStartDate!.Value, c.Saga.AgvData!, 0);
    }

    private EventActivityBinder<SimulatedAgvSaga, ScheduleFinishAction> SetFinishActionHandler()
    {
        return When(FinishActionSchedule.Received)
            .Then(c => _logger.LogDebug("{MessageType} {CorrelationId} received. Current Action: {ActionType}", nameof(ScheduleFinishAction), c.Saga.CorrelationId, c.Saga.Action?.ActionType))
            .IfElse(c => c.Saga.LastActionAborted,
                ifLastActionAborted => ifLastActionAborted
                    .Then(c => c.Saga.LastActionAborted = false),
                ifNormalAction => ifNormalAction.IfElse(c => c.Saga.RemainingDuration > TimeSpan.Zero,
                    ifActionStillRunning =>
                        // reschedule finish action if action still needs time
                        ifActionStillRunning.Schedule(FinishActionSchedule, c => new ScheduleFinishAction(c.Saga.CorrelationId), c => c.Saga.RemainingDuration!.Value),
                    ifActionFinished =>
                        // Transition to Idle first so AgvStatus.Busy is false on the status publish,
                        // then PublishStatusAsync emits the final LastNodeId via UpdateStatus -> UpdatePosition
                        // while Action and TravelSimulator.CurrentEdge are still live (snaps to TargetNode).
                        // FinishActionAsync publishes ActionCompletedByAgv after, so downstream consumers
                        // of the completion event see the AGV idle at its new node.
                        // PublishFinalPositionAsync sets ForcePublish so the snap-to-target reaches the UI
                        // even if the prior scheduled position was within the significance threshold.
                        ifActionFinished.TransitionTo(Idle)
                            .ThenAsync(PublishStatusAsync)
                            .ThenAsync(FinishActionAsync)
                            .Then(c => _logger.LogDebug("{MessageType} {CorrelationId} received", nameof(ScheduleFinishAction), c.Saga.CorrelationId))
                            .ThenAsync(PublishFinalPositionAsync)
                )
            );
    }
    #endregion

    #region Message handlers
    private EventActivityBinder<SimulatedAgvSaga, ScheduleStatusUpdate> SetSendAgvStatusHandler()
    {
        return When(StatusUpdateSchedule.Received)
            .Then(c => { _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ScheduleStatusUpdate), c.Saga.CorrelationId); })
            .ThenAsync(PublishStatusAsync)
            .Schedule(StatusUpdateSchedule, c => new ScheduleStatusUpdate(c.Saga.CorrelationId), GetStatusUpdateInterval);
    }

    private EventActivityBinder<SimulatedAgvSaga, SchedulePositionUpdate> SetSignalPositionHandler()
    {
        return When(SignalPositionSchedule.Received)
            .Then(c => _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(SchedulePositionUpdate), c.Saga.CorrelationId))
            .ThenAsync(PublishPositionAsync)
            .Schedule(SignalPositionSchedule, c => new SchedulePositionUpdate(c.Saga.CorrelationId), GetPositionScheduleInterval);
    }

    private EventActivityBinder<SimulatedAgvSaga, RequestAgvData> SetRequestAgvDataHandler()
    {
        return When(RequestAgvDataEvent)
            .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestAgvData), c.Message.SerialNumber, c.Saga.CorrelationId))
            .ThenAsync(PublishAgvData);
    }

    private EventActivityBinder<SimulatedAgvSaga, RequestAgvZones> SetRequestAgvZonesHandler()
    {
        return When(RequestAgvZonesEvent)
            .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestAgvZones), c.Message.SerialNumber, c.Saga.CorrelationId))
            .ThenAsync(PublishAgvZones);
    }

    private EventActivityBinder<SimulatedAgvSaga, RequestAgvGraph> SetRequestAgvGraphHandler()
    {
        return When(RequestAgvGraphEvent)
            .Then(c => _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(RequestAgvGraph), c.Message.SerialNumber, c.Saga.CorrelationId))
            .ThenAsync(PublishAgvGraph);
    }

    private static void SimulateErrors<T>(BehaviorContext<SimulatedAgvSaga, T> c, List<AgvError> errors) where T : class
    {
        bool hadError = c.Saga.Errors.HasAnyError();
        c.Saga.Errors.AddRange(errors.Where(e => c.Saga.Errors.All(e2 => !e2.IsSameError(e))));
        bool hasError = c.Saga.Errors.HasAnyError();
        if (!hadError && hasError)
        {
            c.Saga.TravelSimulator.Pause();
        }
        else if (hadError && !hasError)
        {
            c.Saga.TravelSimulator.Resume();
        }
    }

    private static void ResetErrors<T>(BehaviorContext<SimulatedAgvSaga, T> c, List<AgvError> errors) where T : class
    {
        bool hadError = c.Saga.Errors.HasAnyError();
        c.Saga.Errors.RemoveAll(error => errors.All(e => !e.IsSameError(error)));
        c.Saga.Errors.AddRange(errors.Where(e => c.Saga.Errors.All(e2 => !e2.IsSameError(e))));
        bool hasError = c.Saga.Errors.HasAnyError();
        if (!hadError && hasError)
        {
            c.Saga.TravelSimulator.Pause();
        }
        else if (hadError && !hasError)
        {
            c.Saga.TravelSimulator.Resume();
        }
    }

    private EventActivityBinder<SimulatedAgvSaga, SimulateAgvError> SimulateAgvErrorHandler()
    {
        return When(SimulateAgvErrorEvent)
            .Then(c => _logger.LogInformation("{MessageType} / {CorrelationId} received", nameof(SimulateAgvError), c.Saga.CorrelationId))
            .Then(c => { SimulateErrors(c, c.Message.AgvErrors); })
            .ThenAsync(PublishAgvError);
    }

    private EventActivityBinder<SimulatedAgvSaga, SimulateActionFailure> SimulateActionFailureHandler()
    {
        return When(SimulateActionFailureEvent)
            .Then(c => _logger.LogInformation("{MessageType} / {CorrelationId} received", nameof(SimulateAgvError), c.Saga.CorrelationId))
            .Then(c => { c.Saga.ShouldFail = true; });
    }

    private EventActivityBinder<SimulatedAgvSaga, ResetAgvErrors> ResetAgvErrorsHandler()
    {
        return When(ResetAgvErrorsEvent)
            .Then(c => _logger.LogDebug("{MessageType} / {CorrelationId} received", nameof(ResetAgvErrors), c.Saga.CorrelationId))
            .Then(c =>
            {
                _logger.LogTrace("Existing errorIds: {ErrorIds}. To be reset: {ErrorIdsToReset}", c.Saga.Errors.Select(e => e.CorrelationId).ToList(), c.Message.ErrorIds);

                // reset all errors if no errorIds are given
                bool resetAllErrors = c.Message.ErrorIds.Count == 0;
                ResetErrors(c, resetAllErrors ? [] : c.Saga.Errors.Where(error => !c.Message.ErrorIds.Contains(error.CorrelationId)).ToList());
            })
            .ThenAsync(PublishAgvError);
    }

    private EventActivityBinder<SimulatedAgvSaga, SimulateCharging> SimulateChargingHandler()
    {
        return When(SimulateChargingEvent)
            .Then(c => _logger.LogDebug("{MessageType} / {CorrelationId} received", nameof(SimulateCharging), c.Saga.CorrelationId))
            .Then(c => c.Saga.SetBatteryLevel(c.Message.Level));
    }

    private EventActivityBinder<SimulatedAgvSaga, SetSimulationSpeedForAgv> SetSimulationSpeedHandler()
    {
        return When(SetSimulationSpeedEvent)
            .Then(c => _logger.LogInformation("SetSimulationSpeed: {SimulationSpeed}x", c.Message.SimulationSpeed))
            .Then(c =>
            {
                c.Saga.SetSimulationSpeed(c.Message.SimulationSpeed);
                SetUpdateIntervals(c.Saga.SimulationSpeed);
            });
    }

    private EventActivityBinder<SimulatedAgvSaga, RemoveAgvFromZone> RemoveAgvFromZoneHandler()
    {
        return When(RemoveAgvFromZoneEvent)
            .Then(c => _logger.LogDebug("{MessageType} / {CorrelationId} received", nameof(RemoveAgvFromZone), c.Saga.CorrelationId))
            .Then(c => c.Saga.AgvRemovedFromZones = true);
    }
    #endregion

    #region Actions
    /// <summary>
    /// Gets the interval [ms] between requests given the number of requests that
    /// are to be scheduled while the agv is on an edge.
    /// </summary>
    /// <param name="requestsOnEdge">Number of requests on the edge.</param>
    /// <param name="lowerBound">The lower bound of the interval [ms].</param>
    /// <param name="upperBound">The upper bound of the interval [ms].</param>
    /// <param name="simulationSpeed">The simulation speed.</param>
    /// <param name="currentEdge">The current edge.</param>
    /// <returns></returns>
    private static int GetRequestIntervalOnEdge(int requestsOnEdge, int lowerBound, int upperBound, int simulationSpeed, BaseEdge? currentEdge)
    {
        int interval = simulationSpeed / requestsOnEdge;
        if (currentEdge != null)
        {
            interval = (int)(currentEdge.EstimatedExitTime - currentEdge.InitialTurnFinishTime).TotalMilliseconds / requestsOnEdge;
        }

        return Math.Clamp(interval, lowerBound, upperBound);
    }

    private TimeSpan GetPositionScheduleInterval(SagaConsumeContext<SimulatedAgvSaga> context)
    {
        var currentEdge = context.Saga.TravelSimulator.GetShortestEdge();
        int interval = GetRequestIntervalOnEdge(12, 100, 10000, context.Saga.SimulationSpeed, currentEdge);
        return TimeSpan.FromMilliseconds(interval);
    }

    private TimeSpan GetStatusUpdateInterval(SagaConsumeContext<SimulatedAgvSaga> context)
    {
        var currentEdge = context.Saga.TravelSimulator.CurrentEdge;
        int interval = GetRequestIntervalOnEdge(2, 200, 2000, context.Saga.SimulationSpeed, currentEdge);
        return TimeSpan.FromMilliseconds(interval);
    }

    private async Task PublishStatusAsync(BehaviorContext<SimulatedAgvSaga> context)
    {
        _logger.LogTrace("Status event from AGV {AgvId} / {CorrelationId}",
            context.Saga.SerialNumber, context.Saga.CorrelationId);
        bool wasOnline = context.Saga.IsOnline;
        context.Saga.UpdateStatus(GetStatusUpdateInterval(context).TotalSeconds, BatteryChargeRate, BatteryDischargeRate); // Update status before each send

        if (context.Saga.IsOnline) // Only send status if AGV is still alive
        {
            await context.Publish(new AgvStatusReceived(
                context.Saga.CorrelationId,
                context.Saga.SerialNumber,
                context.Saga.AgvStatus
            ), context.CancellationToken);
            await PublishAgvWifiMessage(context);
        }

        if (wasOnline && !context.Saga.IsOnline)
        {
            await context.Publish(new AgvLastWillReceived(context.Saga.CorrelationId));
        }
    }

    private Task PublishPositionAsync(BehaviorContext<SimulatedAgvSaga> context) =>
        PublishPositionAsync(context, forcePublish: false);

    private async Task PublishFinalPositionAsync(BehaviorContext<SimulatedAgvSaga> context) =>
        await PublishPositionAsync(context, forcePublish: true);

    private async Task PublishPositionAsync(BehaviorContext<SimulatedAgvSaga> context, bool forcePublish)
    {
        var saga = context.Saga;
        Guid? previousNode = saga.LastNodeId;
        saga.UpdatePosition();
        var position = saga.Position;
        if (position != null)
        {
            _logger.LogTrace("Position event from AGV {AgvId} / {CorrelationId} on edge {Edge}: {Position}",
                saga.SerialNumber, saga.CorrelationId,
                saga.TravelSimulator.CurrentEdge, position);

            await context.Publish(new AgvPositionReceived(
                saga.CorrelationId,
                saga.SerialNumber,
                position
            ) { ForcePublish = forcePublish }, context.CancellationToken);

            if (previousNode != saga.LastNodeId)
            {
                // if node changed, publish status as well to avoid any delays
                await PublishStatusAsync(context);
            }
        }
    }

    private async Task PublishAgvZones(BehaviorContext<SimulatedAgvSaga> context)
    {
        var agvSaga = context.Saga;
        if (agvSaga.AgvGraph != null)
        {
            agvSaga.AgvZones = [];
            await context.Publish(new AgvZonesReceived(agvSaga.CorrelationId, agvSaga.SerialNumber, agvSaga.AgvZones));
        }
    }

    private async Task PublishAgvData(BehaviorContext<SimulatedAgvSaga> context)
    {
        var agvSaga = context.Saga;
        if (agvSaga.AgvData != null)
        {
            await context.Publish(new AgvDataReceived(agvSaga.CorrelationId, agvSaga.SerialNumber, agvSaga.AgvData));
        }
    }

    private async Task PublishAgvGraph(BehaviorContext<SimulatedAgvSaga> context)
    {
        var agvSaga = context.Saga;
        if (agvSaga.AgvGraph != null)
        {
            await context.Publish(new AgvGraphReceived(agvSaga.CorrelationId, agvSaga.SerialNumber, agvSaga.AgvGraph));
        }
    }

    private static void Validate(BehaviorContext<SimulatedAgvSaga, DispatchRouteCommand> context)
    {
        if (context.Message.Action == null)
        {
            throw new ArgumentException("Action data missing.");
        }
        if (context.Message.Route.BaseNodes.Count == 0 && !context.Message.Action.IsMoveToCoordinate)
        {
            throw new ArgumentException("Nodes missing.");
        }
    }

    private static void Validate(BehaviorContext<SimulatedAgvSaga, UpdateDispatchedRouteCommand> context)
    {
        if (context.Message.Action == null)
        {
            throw new ArgumentException("Action data missing.");
        }
        if (context.Message.Route.BaseNodes.Count == 0 && !context.Message.Action.IsMoveToCoordinate)
        {
            throw new ArgumentException("Nodes missing.");
        }
    }

    private static void Validate(BehaviorContext<SimulatedAgvSaga, AbortAndDispatchRouteCommand> context)
    {
        if (context.Message.Action == null)
        {
            throw new ArgumentException("Action data missing.");
        }
        if (context.Message.Route.BaseNodes.Count == 0 && !context.Message.Action.IsMoveToCoordinate)
        {
            throw new ArgumentException("Nodes missing.");
        }
    }

    private async Task FinishActionAsync(BehaviorContext<SimulatedAgvSaga, ScheduleFinishAction> context)
    {
        if (context.Saga.ActionStartDate == null)
        {
            _logger.LogWarning("Action already finished");
            return;
        }
        var action = context.Saga.Action;
        if (action == null)
        {
            _logger.LogError("Could not finish action, AgvAction object missing");
        }
        else
        {
            var agvCorrelationId = context.Saga.CorrelationId;
            _logger.LogInformation("SerialNumber {AgvId} ({CorrelationId}). Finished {ActionType} action with action CorrelationId {ActionCorrelationId} for action group id: {OrderId}",
                context.Saga.SerialNumber, context.Saga.CorrelationId, context.Saga.Action!.ActionType, context.Saga.Action.CorrelationId, context.Saga.Action.ParentId);
            var cargos = new List<Cargo>();
            if (action.ActionType == ActionType.Pick)
            {
                // we just picked up a cargo
                var cargo = new Cargo(Guid.NewGuid(), agvCorrelationId, "Cargo 1", "default", new Point3D(0, 0, 0, 0), new Cuboid(100f, 200f, 50f), "top", 21);
                cargos.Add(cargo);
            }
            if (action.ActionType is ActionType.Pick or ActionType.Drop)
            {
                await WaitForCargoHandlingAsync(context);
                await context.Publish(new AgvLoadStatusReceived(agvCorrelationId, cargos));
            }
            if (context.Saga.ShouldFail)
            {
                _logger.LogInformation("SerialNumber {AgvId} ({CorrelationId}). {ActionType} action with action CorrelationId {ActionCorrelationId} for action group id should fail: {OrderId}",
                    context.Saga.SerialNumber, context.Saga.CorrelationId, context.Saga.Action!.ActionType, context.Saga.Action.CorrelationId, context.Saga.Action.ParentId);
                await context.Publish(new ActionFailedByAgv(action.CorrelationId, agvCorrelationId));
                context.Saga.ShouldFail = false;
                return;
            }
            await context.Publish(new ActionCompletedByAgv(action.CorrelationId, agvCorrelationId));
        }

        // update position last time before the ActionStart time is gone
        context.Saga.UpdatePosition();
        context.Saga.ResetAction();
    }

    private async Task WaitForCargoHandlingAsync(BehaviorContext<SimulatedAgvSaga, ScheduleFinishAction> context)
    {
        var action = context.Saga.Action;
        float delay = action!.ActionType switch
        {
            ActionType.Drop => _options.CargoUnloadingTime,
            ActionType.Pick => _options.CargoLoadingTime,
            _ => 0
        } / (float)context.Saga.SimulationSpeed;

        await Task.Delay(TimeSpan.FromSeconds(delay));
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
                context.Saga.SerialNumber, context.Saga.CorrelationId, action.ActionType, action.ParentId);
            await context.Publish(new ActionAbortedByAgv(action.CorrelationId, context.Saga.CorrelationId));
        }

        context.Saga.AbortAction();
    }

    private async Task PublishAgvError<T>(BehaviorContext<SimulatedAgvSaga, T> context) where T : class
    {
        _logger.LogDebug("Publish {Message} for Agv with Correlation Id: {AgvCorrelationId}", nameof(AgvErrorStatusReceived), context.Saga.CorrelationId);
        await context.Publish(new AgvErrorStatusReceived(context.Saga.CorrelationId, context.Saga.Errors, context.Saga.Position!), context.CancellationToken);
    }

    private readonly Random _random = Random.Shared;

    private async Task PublishAgvWifiMessage(BehaviorContext<SimulatedAgvSaga> context)
    {
        _logger.LogDebug("Publish random wifi message for Agv with Correlation Id: {AgvCorrelationId}", context.Saga.CorrelationId);
        int wifiStrength = _random.Next(70, 90);
        var wifiMessage = new AgvError(context.Saga.CorrelationId, "WIFI", $"Signal strength {wifiStrength}", "", ErrorLevel.Information, "WIFI", ErrorCategory.General, [], DateTime.UtcNow, ErrorFlag.Agv, AgvErrorSource.Agv);
        await context.Publish(new AgvErrorStatusReceived(context.Saga.CorrelationId, [wifiMessage], context.Saga.Position!), context.CancellationToken);
        await context.Publish(new AgvWifiMetricsReceived(context.Saga.CorrelationId, context.Saga.SerialNumber, context.Saga.Position!, new WifiMetrics("ssid", null, null, null, -wifiStrength, null, null, DateTime.UtcNow)));
    }

    private EventActivityBinder<SimulatedAgvSaga> UpdateTimeOfStateTransition(EventActivityBinder<SimulatedAgvSaga> eventActivity)
    {
        return eventActivity.Then(ctx => ctx.Saga.TimeOfStateTransition = _timeProvider.GetUtcNow());
    }
    #endregion
}