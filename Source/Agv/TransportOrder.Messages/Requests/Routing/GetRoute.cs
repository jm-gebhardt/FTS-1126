using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;

/// <summary>
/// Represents the request which returns the route for the given Agv from its current node to the destination node.
/// </summary>
public record GetRoute : CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlation Id of Agv
    /// </summary>
    [UsedImplicitly]
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The start node Id from which the route should be calculated. If null the last node Id of the Agv will be used.
    /// </summary>
    public Guid? StartNodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The destination node Id to which the route should be calculated, starting from the Agvs current node.
    /// </summary>
    public Guid DestinationNodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <inheritdoc cref="GetRoute" />
    /// <param name="agvCorrelationId">Agv correlationId.</param>
    /// <param name="destinationNodeId">The destination node Id to which the route should be calculated, starting from the Agvs current node.</param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the Agv will be used.</param>
    public GetRoute(Guid agvCorrelationId, Guid destinationNodeId, Guid? startNodeId = null)
    {
        CorrelationId = agvCorrelationId;
        StartNodeId = startNodeId;
        DestinationNodeId = destinationNodeId;
    }

    /// <summary>
    /// Deserialization
    /// </summary>
    [UsedImplicitly]
    public GetRoute()
    {
    }
}