using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

/// <summary>
/// Root aggregate for a background editor scene.
/// Owns all canvas objects, layers, and colour palette entries.
/// </summary>
[PublicAPI]
public class Background
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; init; }

    /// <summary>Name of the background.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional reference to the map this background belongs to.</summary>
    public Guid? MapId { get; set; }

    /// <summary>All drawable objects in the scene.</summary>
    public ICollection<BackgroundObject> Objects { get; init; } = [];

    /// <summary>Ordered layer stack controlling render order, visibility, and locking.</summary>
    public ICollection<BackgroundLayer> Layers { get; init; } = [];

    /// <summary>Colour palette entries (line colours and fill colours).</summary>
    public ICollection<BackgroundColorEntry> ColorEntries { get; init; } = [];

    /// <inheritdoc cref="Background"/>
    public Background()
    {
        Id = Guid.NewGuid();
    }

    /// <inheritdoc cref="Background"/>
    public Background(Guid id)
    {
        Id = id;
    }

    /// <summary>Parameterless constructor for EF Core.</summary>
    [UsedImplicitly]
    private Background(bool _)
    {
        Id = Guid.NewGuid();
    }
}
