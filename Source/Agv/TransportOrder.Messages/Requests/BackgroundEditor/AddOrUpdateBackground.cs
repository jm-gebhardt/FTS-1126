using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;

/// <summary>
/// Request to add or update a background editor scene.
/// </summary>
/// <param name="Background">The background domain object to persist.</param>
public record AddOrUpdateBackground(Background Background) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Guid}"/>
    public Guid CorrelationId => Background.Id;
}
