using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.BackgroundEditor;

/// <summary>
/// Configures the TableName-Per-Hierarchy mapping for all <see cref="BackgroundObject"/> subtypes.
/// All concrete types share the <c>BackgroundObjects</c> table; the <c>ObjectType</c>
/// discriminator column identifies the concrete type at runtime.
/// </summary>
public class BackgroundObjectEntityConfiguration : IEntityTypeConfiguration<BackgroundObject>
{
    private const string TableName = "BackgroundObjects";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BackgroundObject> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable(TableName);

        builder.Property(e => e.ExternalId).IsRequired();
        builder.Property(e => e.BackgroundId).IsRequired();
        builder.Property(e => e.X).IsRequired();
        builder.Property(e => e.Y).IsRequired();
        builder.Property(e => e.Width).IsRequired();
        builder.Property(e => e.Height).IsRequired();
        builder.Property(e => e.Rotation).IsRequired();
        builder.Property(e => e.ScaleX).IsRequired();
        builder.Property(e => e.ScaleY).IsRequired();

        builder.HasDiscriminator<string>("ObjectType")
            .HasValue<RectangleObject>("rectangle")
            .HasValue<TriangleObject>("triangle")
            .HasValue<CircleObject>("circle")
            .HasValue<LineObject>("line")
            .HasValue<PolylineObject>("polyline")
            .HasValue<TextObject>("text");

        // Text-specific columns
        builder.Property<string>(nameof(TextObject.Text))
            .HasMaxLength(DbStringLimits.Description)
            .IsRequired(false);

        builder.Property<string>(nameof(TextObject.FontFamily))
            .HasMaxLength(DbStringLimits.Name)
            .IsRequired(false);
    }
}
