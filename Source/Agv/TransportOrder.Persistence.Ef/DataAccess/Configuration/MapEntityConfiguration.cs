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
        builder.Property(e => e.MapId).HasMaxLength(DbStringLimits.Description);

        builder.HasOne(m => m.BaseBackground)
            .WithOne()
            .HasForeignKey<Map>(m => m.BaseBackgroundId)
            .HasPrincipalKey<MapBackground>(m => m.CorrelationId)
            .OnDelete(DeleteBehavior.Cascade);
        // autoinclude for Base/CustomBackground can be removed once the FE uses the new endpoint (meta)
        builder.Navigation(m => m.BaseBackground)
            .UsePropertyAccessMode(PropertyAccessMode.Property)
            .AutoInclude();

        builder.HasOne(m => m.CustomBackground)
            .WithOne()
            .HasForeignKey<Map>(m => m.CustomBackgroundId)
            .HasPrincipalKey<MapBackground>(m => m.CorrelationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(m => m.CustomBackground)
            .UsePropertyAccessMode(PropertyAccessMode.Property)
            .AutoInclude();

        builder.HasMany(m => m.Edges)
            .WithMany(n => n.Maps)
            .UsingEntity("MapEdges");

        builder.HasMany(m => m.Nodes)
            .WithMany(n => n.Maps)
            .UsingEntity("MapNodes");

        builder.HasMany(m => m.MapAgvs)
            .WithOne(ma => ma.Map)
            .HasForeignKey(ma => ma.MapId);

        builder.ConfigureAllProperties(except: [nameof(Map.BaseBackground), nameof(Map.CustomBackground), nameof(Map.Nodes), nameof(Map.Edges), nameof(Map.MapAgvs)]);
    }
}