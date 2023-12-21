using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
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
public record AgvGraphReceived : CorrelatedBy<Guid>, IAgvId
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string AgvId { get; }

    /// <summary>
    /// Agv Graph
    /// </summary>
    public AgvGraph Graph { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId">Unique AGV Correlation ID</param>
    /// <param name="agvId">AGV serial number</param>
    /// <param name="graph">AGV graph</param>
    public AgvGraphReceived(Guid correlationId, string agvId, AgvGraph graph)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        Graph = graph;
    }
}