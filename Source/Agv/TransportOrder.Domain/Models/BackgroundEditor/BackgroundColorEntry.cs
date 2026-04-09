using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

/// <summary>
/// A single colour value in the background editor's colour palette.
/// Stored in a dedicated table; the <see cref="PaletteType"/> discriminator
/// separates line colours from fill colours.
/// </summary>
[PublicAPI]
public class BackgroundColorEntry
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; init; }

    /// <summary>Foreign key to the owning <see cref="Background"/>.</summary>
    public Guid BackgroundId { get; set; }

    /// <summary>Whether this entry belongs to the line-colour or fill-colour palette.</summary>
    public ColorPaletteType PaletteType { get; set; }

    /// <summary>Colour value encoded as a 24-bit RGB integer.</summary>
    public int Value { get; set; }

    /// <inheritdoc cref="BackgroundColorEntry"/>
    public BackgroundColorEntry(Guid backgroundId, ColorPaletteType paletteType, int value)
    {
        Id = Guid.NewGuid();
        BackgroundId = backgroundId;
        PaletteType = paletteType;
        Value = value;
    }

    /// <summary>Parameterless constructor for EF Core.</summary>
    [UsedImplicitly]
    private BackgroundColorEntry()
    {
        Id = Guid.NewGuid();
    }
}
