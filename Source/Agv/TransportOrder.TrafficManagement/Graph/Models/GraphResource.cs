
namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// Represents a resource in the graph.
/// </summary>
public abstract class GraphResource(Guid id, string name, string shortName)
{
    /// <summary>
    /// Unique identifier for the graph resource.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    /// Name of the graph resource.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Description of the graph resource.
    /// </summary>
    public string ShortName { get; } = shortName;
}