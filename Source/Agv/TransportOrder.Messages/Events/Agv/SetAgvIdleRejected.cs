using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv reservation request was rejected
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
[PublicAPI]
public record SetAgvIdleRejected(Guid CorrelationId) : CorrelatedBy<Guid>;