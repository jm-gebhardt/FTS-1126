using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

/// <summary>
/// A named layer that groups canvas objects and controls their visibility and editability.
/// </summary>
[PublicAPI]
public class BackgroundLayer
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; init; }

    /// <summary>The numeric id from the source JSON document.</summary>
    public int ExternalId { get; set; }

    /// <summary>Foreign key to the owning <see cref="Background"/>.</summary>
    public Guid BackgroundId { get; set; }

    /// <summary>Human-readable display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>When <c>false</c> the layer's objects are hidden in the editor.</summary>
    public bool Visible { get; set; }

    /// <summary>When <c>true</c> the layer's objects cannot be modified in the editor.</summary>
    public bool Locked { get; set; }

    /// <summary>Objects that belong to this layer.</summary>
    public ICollection<BackgroundObject> Objects { get; init; } = [];

    /// <inheritdoc cref="BackgroundLayer"/>
    public BackgroundLayer(Guid backgroundId, int externalId, string name, bool visible, bool locked)
    {
        Id = Guid.NewGuid();
        BackgroundId = backgroundId;
        ExternalId = externalId;
        Name = name;
        Visible = visible;
        Locked = locked;
    }

    /// <summary>Parameterless constructor for EF Core.</summary>
    [UsedImplicitly]
    private BackgroundLayer()
    {
        Id = Guid.NewGuid();
    }
}
