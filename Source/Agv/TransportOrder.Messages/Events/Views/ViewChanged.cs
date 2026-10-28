using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Views;

/// <summary>
/// Represents the event raised when a view was created or changed.
/// </summary>
/// <param name="View">The created or changed view.</param>
public record ViewChanged(View View) : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId => View.Id;
}