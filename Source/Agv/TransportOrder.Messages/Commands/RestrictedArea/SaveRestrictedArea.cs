using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;

/// <summary>
/// Provides a command to save a restricted area in the db.
/// </summary>
/// <param name="RestrictedArea"></param>
public record SaveRestrictedArea(Domain.Models.RestrictedArea.RestrictedArea RestrictedArea) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Guid}"/>>
    public Guid CorrelationId => RestrictedArea.Id;
}