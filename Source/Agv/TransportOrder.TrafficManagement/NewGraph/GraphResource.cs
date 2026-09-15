using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.NewGraph;

/// <summary>
/// Represents a resource in the graph.
/// </summary>
[PublicAPI]
public abstract class GraphResource
{
    private readonly List<RoutingResource> _children = [];

    /// <summary>
    /// Unique identifier for the graph resource.
    /// </summary>
    public Guid Id { get; }  // unique identifier derived from LifId + resourceType + nodeId/edgeId/stationId      e.g.: ToGuid("Karis/Node/N1")  ToGuid("Still/Node/N1")?

    /// <summary>
    /// Name of the graph resource.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Description of the graph resource.
    /// </summary>
    public string ShortName { get; }

    /// <summary>
    /// Collection of child routing resources.
    /// </summary>
    // ReSharper disable once MemberCanBeProtected.Global
    public IReadOnlyCollection<RoutingResource> Children => _children;

    /// <inheritdoc cref="GraphResource"/>
    protected GraphResource(Guid id, string name, string shortName)
    {
        Id = id;
        Name = name;
        ShortName = shortName;
    }

    /// <summary>
    /// Attaches a child routing resource. Not backed by an EF relation (see <c>RoutingResourceEntityConfiguration</c>) -
    /// used by GraphRepository to assemble the aggregate after resolving Parent/Children via an explicit join.
    /// </summary>
    public void AddChild(RoutingResource child)
    {
        _children.Add(child);
    }
}