using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;

/// <summary>
/// Signals that an action has been started
/// </summary>
[PublicAPI]
public record ActionStartedEvent(Guid CorrelationId, Guid ParentId) : CorrelatedBy<Guid>, IActionEventParent<Guid>;

/// <summary>
/// Signals that an action has been completed
/// </summary>
[PublicAPI]
public record ActionCompletedEvent(Guid CorrelationId, Guid ParentId) : CorrelatedBy<Guid>, IActionEventParent<Guid>;

/// <summary>
/// Signals that an action has failed
/// </summary>
[PublicAPI]
public record ActionFailedEvent(Guid CorrelationId, Guid ParentId) : CorrelatedBy<Guid>, IActionEventParent<Guid>;

/// <summary>
/// Signals that an action has been cancelled
/// </summary>
[PublicAPI]
public record ActionCancelledEvent(Guid CorrelationId, Guid ParentId) : CorrelatedBy<Guid>, IActionEventParent<Guid>;

/// <summary>
/// Signals that an action cancelation has failed 
/// </summary>
[PublicAPI]
public record ActionCancelFailedEvent(Guid CorrelationId, Guid ParentId) : CorrelatedBy<Guid>, IActionEventParent<Guid>;
