using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that one or more properties of an AGV has changed
/// </summary>
/// <param name="Agv">AGV properties</param>
/// <param name="CorrelationId">The correlationId of the AGV</param>
[JetBrains.Annotations.PublicAPI]
public record AgvPropertyChanged(IAgv Agv, Guid CorrelationId) : CorrelatedBy<Guid>;