using MassTransit;

namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the AcceptAgv request
/// </summary>
public record AcceptAgvCommand(Guid CorrelationId) : CorrelatedBy<Guid>;