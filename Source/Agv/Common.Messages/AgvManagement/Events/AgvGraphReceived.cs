using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the graph of an AGV.
/// </summary><remarks>
/// This data structure is optimized for
/// information purpose by keeping additional information like coordinates and for
/// routing algorithms by having information like metric.
/// </remarks>
[PublicAPI]
public record AgvGraphReceived : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// Agv Graph
    /// </summary>
    public AgvGraph Graph { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId">Unique AGV Correlation ID</param>
    /// <param name="serialNumber">The serial number of the agv</param>
    /// <param name="graph">AGV graph</param>
    public AgvGraphReceived(Guid correlationId, string serialNumber, AgvGraph graph)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Graph = graph;
    }
}