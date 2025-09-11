using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the EmitRequestData request
/// </summary>
public record RequestAgvData : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The user, who emitted the request.
    /// </summary>
    public string User { get; }

    /// <inheritdoc cref="RequestAgvData"/>
    public RequestAgvData(Guid correlationId, string user)
    {
        CorrelationId = correlationId;
        User = user;
    }
}