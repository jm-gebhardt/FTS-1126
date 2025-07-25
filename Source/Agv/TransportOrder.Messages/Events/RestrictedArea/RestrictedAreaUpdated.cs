using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.RestrictedArea;

/// <summary>
/// Represents the event raised when a restricted area has been updated.
/// </summary>
/// <param name="RestrictedArea">The updated restricted area.</param>
public record RestrictedAreaUpdated(Domain.Models.RestrictedArea.RestrictedArea RestrictedArea) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Guid}"/>>
    public Guid CorrelationId => RestrictedArea.Id;
}