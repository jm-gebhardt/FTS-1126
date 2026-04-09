using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;

/// <summary>
/// Provides a request to delete a background.
/// </summary>
/// <param name="Id">The id of the background.</param>
public record DeleteBackgroundScene(Guid Id) : CorrelatedBy<Guid>
{
    /// <summary>
    /// CorrelationId -> Name of the background
    /// </summary>
    public Guid CorrelationId => Id;
}