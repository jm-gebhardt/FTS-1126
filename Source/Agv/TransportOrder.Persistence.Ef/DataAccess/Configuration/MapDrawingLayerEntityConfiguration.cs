using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// EF Core configuration for MapDrawingLayer entity.
/// </summary>
public class MapDrawingLayerEntityConfiguration : IEntityTypeConfiguration<MapDrawingLayer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MapDrawingLayer> builder)
    {
        builder.ToTable("MapDrawingLayers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MapCorrelationId)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsConcurrencyToken();

        builder.Property(e => e.ElementsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.LastModified)
            .IsRequired();

        builder.HasIndex(e => e.MapCorrelationId)
            .IsUnique();
    }
}
