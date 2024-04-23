using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Represents the AcceptAgv request
/// </summary>
public record AcceptAgvCommand(Guid CorrelationId) : CorrelatedBy<Guid>;