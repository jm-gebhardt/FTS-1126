using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

/// <summary>
/// A vertex in a line or polyline object, stored in object-local coordinates.
/// </summary>
[PublicAPI]
public class BackgroundObjectPoint
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; init; }

    /// <summary>Foreign key to the owning <see cref="BackgroundObject"/>.</summary>
    public Guid ObjectId { get; set; }

    /// <summary>Zero-based position of this vertex in the point list.</summary>
    public int Index { get; set; }

    /// <summary>Horizontal coordinate relative to the object's origin.</summary>
    public double X { get; set; }

    /// <summary>Vertical coordinate relative to the object's origin.</summary>
    public double Y { get; set; }

    /// <inheritdoc cref="BackgroundObjectPoint"/>
    public BackgroundObjectPoint(Guid objectId, int index, double x, double y)
    {
        Id = Guid.NewGuid();
        ObjectId = objectId;
        Index = index;
        X = x;
        Y = y;
    }

    /// <summary>Parameterless constructor for EF Core.</summary>
    [UsedImplicitly]
    private BackgroundObjectPoint()
    {
        Id = Guid.NewGuid();
    }
}
