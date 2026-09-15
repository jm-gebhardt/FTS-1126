using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// A node in the routing graph. Each routing node has an associated AGV type, a load state, and a theta value.
/// </summary>
[PublicAPI]
public record RoutingNode : RoutingResource
{
    /// <summary>
    /// The load state of the routing node.
    /// </summary>
    public Guid LoadStateId { get; init; }

    /// <summary>
    /// The theta value of the routing node, which may represent the orientation of the AGV.
    /// </summary>
    public double Theta { get; init; }

    /// <inheritdoc cref="RoutingNode"/>
    public RoutingNode(Guid id, Guid agvTypeId, Guid loadStateId, double theta) : base(id, agvTypeId)
    {
        LoadStateId = loadStateId;
        Theta = theta;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public RoutingNode()
    {
    }
}