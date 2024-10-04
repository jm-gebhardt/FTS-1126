using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// A graph as exported from Karis.
/// </summary>
[PublicAPI]
public class ExportedGraph
{
    /// <summary>
    /// Provides nodes.
    /// </summary>
    public ICollection<ExportedNode> Nodes { get; set; } = [];

    /// <summary>
    /// Provides segments.
    /// </summary>
    public ICollection<ExportedSegment> Segments { get; set; } = [];
}

/// <summary>
/// A segment of the exported graph
/// </summary>
[PublicAPI]
public record ExportedSegment
{
    /// <summary>
    /// Control points.
    /// </summary>
    public ICollection<ExportedControlPoint> Points { get; set; } = [];

    /// <summary>
    /// The name of the segment.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The start node of the segment.
    /// </summary>
    [JsonPropertyName("start_node")]
    public string StartNode { get; set; } = "";

    /// <summary>
    /// The end node of the segment.
    /// </summary>
    [JsonPropertyName("end_node")]
    public string EndNode { get; set; } = "";
}

/// <summary>
/// A control point in the exported segment.
/// </summary>
/// <param name="X">The x coordinate of the control point</param>
/// <param name="Y">The y coordinate of the control point</param>
[PublicAPI]
public record ExportedControlPoint(double X, double Y);

/// <summary>
/// A node as exported from Karis
/// </summary>
[PublicAPI]
public class ExportedNode
{
    /// <summary>
    /// The name of the node.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The x-coordinate of the node.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// The y-coordinate of the node.
    /// </summary>
    public double Y { get; set; }

    /// <inheritdoc cref="ExportedNode"/>
    public ExportedNode(string name, double x, double y)
    {
        Name = name;
        X = x;
        Y = y;
    }
}