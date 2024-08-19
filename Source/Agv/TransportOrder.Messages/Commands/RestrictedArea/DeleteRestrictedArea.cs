using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;

/// <summary>
/// Provides a command to delete a restricted area.
/// </summary>
/// <param name="RestrictedAreaId">The id of the restricted area.</param>
public record DeleteRestrictedArea(Guid RestrictedAreaId): CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Guid}"/>>
    public Guid CorrelationId => RestrictedAreaId;
}