using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Views;

/// <summary>
/// A rectangular area of the graph in [m], in graph coordinates.
/// </summary>
/// <param name="MinX">The smallest x of the area.</param>
/// <param name="MinY">The smallest y of the area.</param>
/// <param name="MaxX">The largest x of the area.</param>
/// <param name="MaxY">The largest y of the area.</param>
[PublicAPI]
public record ViewArea(double MinX, double MinY, double MaxX, double MaxY)
{
    /// <summary>
    /// Returns the area enlarged by the given margin on every side.
    /// </summary>
    /// <param name="margin">The margin in [m].</param>
    public ViewArea Enlarge(double margin)
    {
        return new ViewArea(MinX - margin, MinY - margin, MaxX + margin, MaxY + margin);
    }

    /// <summary>
    /// Returns the smallest area containing this and the other area.
    /// </summary>
    /// <param name="other">The other area.</param>
    public ViewArea Union(ViewArea other)
    {
        return new ViewArea(Math.Min(MinX, other.MinX),
            Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX),
            Math.Max(MaxY, other.MaxY));
    }
}