using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the current position of an AGV has changed
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="AgvState">The new state</param>
/// <param name="PreviousState">Previous state of the agv</param>
/// <param name="Agv">The full Agv</param>
[PublicAPI]
public record AgvStateChanged(Guid CorrelationId, AgvState AgvState, AgvState? PreviousState, MessageContent.Agv Agv) : CorrelatedBy<Guid>;