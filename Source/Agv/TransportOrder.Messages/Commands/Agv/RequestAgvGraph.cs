using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the EmitRequestGraph request which returns AgvGraph
/// </summary>
public record RequestAgvGraph : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The user, who emitted the request.
    /// </summary>
    public string User { get; }

    /// <inheritdoc cref="RequestAgvGraph"/>
    public RequestAgvGraph(Guid correlationId, string user)
    {
        CorrelationId = correlationId;
        User = user;
    }
}