using System.Text.Json;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// EF Core configuration for MapDrawingLayer entity.
/// </summary>
public class MapDrawingLayerEntityConfiguration : IEntityTypeConfiguration<MapDrawingLayer>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MapDrawingLayer> builder)
    {
        builder.ToTable("MapDrawingLayers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MapCorrelationId)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsConcurrencyToken();

        builder.Property(e => e.LastModified)
            .IsRequired();

        builder.HasIndex(e => e.MapCorrelationId)
            .IsUnique();

        // Configure Elements as a JSON column using value conversion
        // The polymorphism is handled by System.Text.Json via [JsonPolymorphic] attributes on DrawingElement
        var converter = new ValueConverter<IList<DrawingElement>, string>(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v => JsonSerializer.Deserialize<IList<DrawingElement>>(v, JsonOptions) ?? new List<DrawingElement>()
        );

        var comparer = new ValueComparer<IList<DrawingElement>>(
            (c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) == JsonSerializer.Serialize(c2, JsonOptions),
            c => c == null ? 0 : JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
            c => JsonSerializer.Deserialize<IList<DrawingElement>>(JsonSerializer.Serialize(c, JsonOptions), JsonOptions) ?? new List<DrawingElement>()
        );

        builder.Property(e => e.Elements)
            .HasColumnType("nvarchar(max)")
            .HasConversion(converter)
            .Metadata.SetValueComparer(comparer);
    }
}
