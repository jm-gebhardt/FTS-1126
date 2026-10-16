using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Signals that an action has been started by an agv.
/// </summary>
[PublicAPI]
public record ActionStartedByAgv(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent, IAgvActionEvent;

/// <summary>
/// Signals that an action has been updated by an agv.
/// </summary>
[PublicAPI]
public record ActionUpdatedByAgv(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent, IAgvActionEvent;

/// <summary>
/// Signals that an action has been completed by an agv.
/// </summary>
/// <param name="CorrelationId">The action correlation id.</param>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
/// <param name="Revision">The revision of the action the agv completed, null if the device communication does not track it.</param>
[PublicAPI]
public record ActionCompletedByAgv(Guid CorrelationId, Guid AgvCorrelationId, int? Revision = null) : IActionEventParent;

/// <summary>
/// Signals that an action has failed by an agv.
/// </summary>
[PublicAPI]
public record ActionFailedByAgv(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action has been aborted by an agv.
/// </summary>
[PublicAPI]
public record ActionAbortedByAgv(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action abortion has failed 
/// </summary>
[PublicAPI]
public record ActionAbortionFailedByAgv(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action abortion has been completed by an agv.
/// </summary>
[PublicAPI]
public record ActionAbortionCompletedByAgv(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action the agv runs on a node/edge or as an instant action changed its status.
/// </summary>
public record VehicleActionStatusChanged(Guid CorrelationId, Guid AgvCorrelationId, string ActionType, VehicleActionStatus ActionStatus) : CorrelatedBy<Guid>;