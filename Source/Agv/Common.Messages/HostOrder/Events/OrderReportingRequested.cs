using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.HostOrder.Events;

/// <summary>
/// Provides a command to report an order to the conveyor.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="TargetStation">The target station.</param>
/// <param name="DropTarget">The drop target lane.</param>
/// <param name="PickTarget">The pick target lane.</param>
/// <param name="PickOrderCorrelationId">The correlation id of the pick order, if it is a Doppelspiel.</param>
/// <param name="DroppedHu">The handling unit of the dropped pallet.</param>
[PublicAPI]
public record OrderReportingRequested(Guid CorrelationId, string TargetStation, string? DropTarget, string? PickTarget, string? DroppedHu, Guid? PickOrderCorrelationId) : CorrelatedBy<Guid>;