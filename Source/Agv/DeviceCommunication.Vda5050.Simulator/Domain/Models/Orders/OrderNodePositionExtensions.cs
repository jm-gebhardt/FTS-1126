using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;

/// <summary>
/// Extension methods for <see cref="OrderNodePosition"/>.
/// </summary>
public static class OrderNodePositionExtensions
{
    /// <summary>
    /// Converts this <see cref="OrderNodePosition"/> to a <see cref="PolygonPoint"/>.
    /// </summary>
    /// <param name="position">The <see cref="OrderNodePosition"/> to convert.</param>
    /// <returns>A <see cref="PolygonPoint"/> with the same X and Y coordinates.</returns>
    public static PolygonPoint ToPolygonPoint(this OrderNodePosition position)
    {
        return new PolygonPoint(position.X, position.Y);
    }
}
