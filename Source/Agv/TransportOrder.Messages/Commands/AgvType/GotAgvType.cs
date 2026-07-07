using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;

/// <summary>
/// Represents the response to the <see cref="GetAgvType"/> request.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
/// <param name="AgvType">The agv type.</param>
public record GotAgvType(Guid CorrelationId, AgvTypeReadModel AgvType) : CorrelatedBy<Guid>;