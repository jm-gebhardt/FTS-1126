using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;

/// <summary>
/// Provides a command to update a restricted area.
/// </summary>
/// <param name="RestrictedArea">The updated restricted area.</param>
public record UpdateRestrictedArea(Domain.Models.RestrictedArea.RestrictedArea RestrictedArea) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Guid}"/>>
    public Guid CorrelationId => RestrictedArea.Id;
}