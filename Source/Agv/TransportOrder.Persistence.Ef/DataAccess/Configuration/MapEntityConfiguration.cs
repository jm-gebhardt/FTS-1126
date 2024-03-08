using Gebhardt.StoreWare.Agv.Common.Persistence.Ef;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class MapEntityConfiguration : IEntityTypeConfiguration<Map>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Map> builder)
    {
        builder.HasKey(e => e.CorrelationId);

        builder.HasMany(m => m.Edges)
            .WithMany(n => n.Maps)
            .UsingEntity("MapEdges");

        builder.HasMany(m => m.Nodes)
            .WithMany(n => n.Maps)
            .UsingEntity("MapNodes");

        builder.HasMany(m => m.MapAgvs)
            .WithOne(ma => ma.Map)
            .HasForeignKey(ma => ma.MapId);

        builder.ConfigureAllProperties(except: new[] { nameof(Map.Nodes), nameof(Map.Edges), nameof(Map.MapAgvs) });
    }
}