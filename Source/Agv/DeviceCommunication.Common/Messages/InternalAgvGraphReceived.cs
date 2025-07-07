using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Common.Messages;

/// <summary>
/// An event reporting the graph of an AGV, only to be used internally in DeviceCommunication.
/// </summary>
[PublicAPI]
public record InternalAgvGraphReceived : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// Agv Graph
    /// </summary>
    public AgvGraph Graph { get; }

    /// <inheritdoc cref="InternalAgvGraphReceived"/>
    public InternalAgvGraphReceived(Guid correlationId, string serialNumber, AgvGraph graph)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Graph = graph;
    }
}