using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;

/// <summary>
/// Represents the response to the <see cref="GetAllAgvTypes"/> request.
/// </summary>
/// <param name="CorrelationId">The correlation id of the request.</param>
/// <param name="AgvTypes">The agv types.</param>
public record GotAgvTypes(Guid CorrelationId, ICollection<AgvTypeReadModel> AgvTypes) : CorrelatedBy<Guid>;