using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class MapEntityConfiguration : IEntityTypeConfiguration<Map>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Map> builder)
    {
        builder.HasKey(e => e.CorrelationId);
        // TODO: Rename to MapName incl. xml docs
        builder.Property(e => e.MapId).HasMaxLength(DbStringLimits.Description);

        builder.OwnsOne(m => m.BaseBackground);
        builder.OwnsOne(m => m.CustomBackground);

        builder.HasMany(m => m.Edges)
            .WithMany(n => n.Maps)
            .UsingEntity("MapEdges");

        builder.HasMany(m => m.Nodes)
            .WithMany(n => n.Maps)
            .UsingEntity("MapNodes");

        builder.HasMany(m => m.MapAgvs)
            .WithOne(ma => ma.Map)
            .HasForeignKey(ma => ma.MapId);

        builder.ConfigureAllProperties(except: [nameof(Map.BaseBackground), nameof(Map.CustomBackground), nameof(Map.Nodes), nameof(Map.Edges), nameof(Map.MapAgvs), nameof(Map.BaseBackgroundId), nameof(Map.CustomBackgroundId)]);
    }
}