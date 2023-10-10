using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that one or more properties of an AGV has changed
/// </summary>
/// <param name="Agv">AGV properties</param>
/// <param name="CorrelationId">The correlationId of the AGV</param>
[PublicAPI]
public record AgvPropertyChanged(MessageContent.Agv Agv, Guid CorrelationId) : CorrelatedBy<Guid>;