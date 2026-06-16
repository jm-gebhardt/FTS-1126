using System.Text.Json;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class ResourceBoundingBoxEntityConfiguration : IEntityTypeConfiguration<ResourceBoundingBox>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ResourceBoundingBox> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.AgvType).HasMaxLength(DbStringLimits.Name);
        builder.Property(o => o.ResourceType).HasConversion<int>();
        builder.Property(o => o.Polygon)
            .HasConversion(new ValueConverter<Polygon, string>(
                v => JsonSerializer.Serialize(v.Points, (JsonSerializerOptions?)null),
                v => new Polygon(JsonSerializer.Deserialize<List<PolygonPoint>>(v, (JsonSerializerOptions?)null)!)))
            .Metadata.SetValueComparer(new ValueComparer<Polygon>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.Points.SequenceEqual(b.Points)),
                a => a.Points.Aggregate(0, (h, p) => HashCode.Combine(h, p.GetHashCode())),
                a => new Polygon(a.Points.ToList())));

        builder.HasIndex(o => new { o.ResourceId, o.AgvType })
            .HasDatabaseName("IX_ResourceBoundingBox_ResourceId_AgvType");
        builder.HasIndex(o => o.AgvType)
            .HasDatabaseName("IX_ResourceBoundingBox_AgvType");
    }
}