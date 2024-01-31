using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Command to request the assignment of an AGV for a specific order.
/// </summary>
[PublicAPI]
public record RequestAgvGraph : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId">Unique AGV Correlation ID</param>
    /// <param name="serialNumber">AGV serial number</param>
    public RequestAgvGraph(Guid correlationId, string serialNumber)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
    }
}