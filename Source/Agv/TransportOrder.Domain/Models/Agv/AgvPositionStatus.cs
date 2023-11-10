using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Model for the status of the agv position.
/// </summary>
[PublicAPI]
public record AgvPositionStatus
{
    /// <summary>The correlationId of the new agv position.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Identifier of the agv.</summary>
    public string AgvId { get; init; }

    /// <summary>The current agv position.</summary>
    public AgvPosition AgvPosition { get; init; }

    /// <summary>When the Status was created</summary>
    public DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="AgvPositionStatus"/>
    /// <param name="correlationId">The correlationId of the new agv position.</param>
    /// <param name="agvId">Identifier of the agv.</param>
    /// <param name="agvPosition">The current agv position.</param>
    public AgvPositionStatus(Guid correlationId, string agvId, AgvPosition agvPosition)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        AgvPosition = agvPosition;
        CreatedDate = DateTime.UtcNow;
    }
}