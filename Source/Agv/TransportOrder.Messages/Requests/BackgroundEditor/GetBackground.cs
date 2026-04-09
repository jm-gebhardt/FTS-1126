using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;

/// <summary>
/// Provides a request to get a background.
/// </summary>
/// <param name="Id">The id of the background is on.</param>
public record GetBackground(Guid Id) : CorrelatedBy<Guid>
{
    /// <summary>
    /// CorrelationId -> Name of the background
    /// </summary>
    public Guid CorrelationId => Id;
}