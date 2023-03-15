using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;

/// <summary>
/// Signals that an action has been started
/// </summary>
[PublicAPI]
public record ActionStartedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Signals that an action has been completed
/// </summary>
[PublicAPI]
public record ActionCompletedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Signals that an action has failed
/// </summary>
[PublicAPI]
public record ActionFailedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Signals that an action has been cancelled
/// </summary>
[PublicAPI]
public record ActionCancelledEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Signals that an action cancelation has failed 
/// </summary>
[PublicAPI]
public record ActionCancelFailedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
