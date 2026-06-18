using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Position of a node.
/// </summary>
[PublicAPI]
public class NodePosition
{
    /// <inheritdoc cref="NodePosition"/>
    public NodePosition(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// X coordinate of the node in meters. Range: [float64.min ... float64.max]
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate of the node in meters. Range: [float64.min ... float64.max]
    /// </summary>
    public double Y { get; set; }
}
