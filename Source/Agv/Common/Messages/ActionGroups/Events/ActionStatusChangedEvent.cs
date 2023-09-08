using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;

/// <summary>
/// Signals that an action has been started
/// </summary>
[PublicAPI]
public record ActionStartedEvent(Guid CorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action has been completed
/// </summary>
[PublicAPI]
public record ActionCompletedEvent(Guid CorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action has failed
/// </summary>
[PublicAPI]
public record ActionFailedEvent(Guid CorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action has been cancelled
/// </summary>
[PublicAPI]
public record ActionCancelledEvent(Guid CorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action cancellation has failed 
/// </summary>
[PublicAPI]
public record ActionCancelFailedEvent(Guid CorrelationId) : IActionEventParent;