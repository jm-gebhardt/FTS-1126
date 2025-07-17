using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised, when an agv is pushed in joystick or push mode by the operator.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record AgvSetToPushOrJoystickMode(Guid CorrelationId): DatedMessage, CorrelatedBy<Guid>;