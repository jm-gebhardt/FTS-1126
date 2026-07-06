
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;

/// <summary>
/// Provides a command to acknowledge an agv type.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
public record AcknowledgeAgvType(Guid CorrelationId) : CorrelatedBy<Guid>;
    
