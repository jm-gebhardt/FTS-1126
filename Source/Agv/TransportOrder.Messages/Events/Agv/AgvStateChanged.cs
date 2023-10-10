using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the current position of an AGV has changed
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv</param>
/// <param name="Agv">Agv with the new state</param>
/// <param name="PreviousState">Previous state of the agv</param>
[PublicAPI]
public record AgvStateChanged(Guid CorrelationId, MessageContent.Agv Agv, AgvState? PreviousState) : CorrelatedBy<Guid>;