using Gebhardt.StoreWare.Agv.Common.Contracts;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Command to request the assignment of an AGV for a specific order.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record RequestAgvGraph : CorrelatedBy<Guid>, IAgvId
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string AgvId { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId">Unique AGV Correlation ID</param>
    /// <param name="agvId">AGV serial number</param>
    public RequestAgvGraph(Guid correlationId, string agvId)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
    }
}