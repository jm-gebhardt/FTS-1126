using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Signals that an action has been started
/// </summary>
[PublicAPI]
public record ActionStarted(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent, IAgvActionEvent;

/// <summary>
/// Signals that an action has been updated
/// </summary>
[PublicAPI]
public record ActionUpdated(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent, IAgvActionEvent;

/// <summary>
/// Signals that an action has been completed
/// </summary>
[PublicAPI]
public record ActionCompleted(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action has failed
/// </summary>
[PublicAPI]
public record ActionFailed(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action has been canceled
/// </summary>
[PublicAPI]
public record ActionCanceled(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action cancellation has failed 
/// </summary>
[PublicAPI]
public record ActionCancelFailed(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;

/// <summary>
/// Signals that an action has been aborted.
/// </summary>
[PublicAPI]
public record ActionAbortionCompleted(Guid CorrelationId, Guid AgvCorrelationId) : IActionEventParent;