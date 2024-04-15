using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Messages.Contracts;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Model for the status of the agv position.
/// </summary>
[PublicAPI]
public record AgvPositionStatus : IAgv
{
    /// <inheritdoc />
    public Guid CorrelationId { get; init; }

    /// <inheritdoc />
    public string SerialNumber { get; init; }

    /// <summary>The current agv position.</summary>
    public AgvPosition AgvPosition { get; init; }

    /// <summary>When the Status was created</summary>
    public DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="AgvPositionStatus"/>
    /// <param name="correlationId">The correlationId of the new agv position.</param>
    /// <param name="serialNumber">Identifier of the agv.</param>
    /// <param name="agvPosition">The current agv position.</param>
    public AgvPositionStatus(Guid correlationId, string serialNumber, AgvPosition agvPosition)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        AgvPosition = agvPosition;
        CreatedDate = DateTime.UtcNow;
    }
}