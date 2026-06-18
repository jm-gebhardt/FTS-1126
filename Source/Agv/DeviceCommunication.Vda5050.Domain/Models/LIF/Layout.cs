using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// A lif layout.
/// </summary>
[PublicAPI]
public class Layout
{
    /// <inheritdoc cref="Layout"/>
    public Layout(
        Guid id,
        string layoutId,
        string layoutVersion,
        ICollection<Edge> edges,
        ICollection<Node> nodes,
        ICollection<Station> stations,
        string? layoutDescription = null,
        string? layoutLevelId = null,
        string? layoutName = null)
    {
        Id = id;
        LayoutId = layoutId;
        LayoutVersion = layoutVersion;
        Edges = edges;
        Nodes = nodes;
        Stations = stations;
        LayoutDescription = layoutDescription;
        LayoutLevelId = layoutLevelId;
        LayoutName = layoutName;
    }

    /// <inheritdoc cref="Layout"/>
    public Layout()
    {
    }

    /// <summary>
    /// The layout unique id.
    /// </summary>
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// List of edges in the layout. Edges represent paths between nodes.
    /// </summary>
    public ICollection<Edge> Edges { get; set; } = [];

    /// <summary>
    /// Description of the layout. Optional.
    /// </summary>
    public string? LayoutDescription { get; set; }

    /// <summary>
    /// Unique identifier for the layout.
    /// </summary>
    public string LayoutId { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the layout level.
    /// </summary>
    public string? LayoutLevelId { get; set; }

    /// <summary>
    /// Name of the layout.
    /// </summary>
    public string? LayoutName { get; set; }

    /// <summary>
    /// Version number of the layout. It is suggested that this be an integer, represented as a string, incremented with each change, starting at 1.
    /// </summary>
    public string LayoutVersion { get; set; } = string.Empty;

    /// <summary>
    /// List of nodes in the layout. Nodes are locations where vehicles can navigate to.
    /// </summary>
    public ICollection<Node> Nodes { get; set; } = [];

    /// <summary>
    /// List of stations in the layout where vehicles perform specific actions.
    /// </summary>
    public ICollection<Station> Stations { get; set; } = [];

    /// <summary>
    /// Get vehicle types supported by the layout.
    /// </summary>
    /// <returns>VehicleTypeIds included in the layouts.</returns>
    public ICollection<string> GetSupportedVehicleTypes()
    {
        List<string> nodeVehicles = Nodes.SelectMany(n => n.GetSupportedVehicleTypes()).ToList();
        List<string> edgeVehicles = Edges.SelectMany(e => e.GetSupportedVehicleTypes()).ToList();
        List<string> vehicles = [.. nodeVehicles, .. edgeVehicles];
        return vehicles.Distinct().ToList();
    }
}
