using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

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
    /// The identifier of the AGV type this graph belongs to, or null if unknown.
    /// </summary>
    public string? AgvTypeIdentifier { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId">Unique AGV Correlation ID</param>
    /// <param name="serialNumber">The serial number of the agv</param>
    /// <param name="graph">AGV graph</param>
    /// <param name="agvTypeIdentifier">The identifier of the AGV type this graph belongs to, or null if unknown.</param>
    public AgvGraphReceived(Guid correlationId, string serialNumber, AgvGraph graph, string? agvTypeIdentifier = null)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Graph = graph;
        AgvTypeIdentifier = agvTypeIdentifier;
    }
}