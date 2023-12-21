using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="Map"/> Entity Framework Mapping
/// </summary>
public class MapClassMap : ClassMap<Map>
{
    /// <summary>
    /// <see cref="Map" /> entity configurations
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<Map> entity, ModelBuilder model)
    {
        entity.HasKey(e => e.CorrelationId);

        entity.HasMany(m => m.Edges)
            .WithMany(n => n.Maps)
            .UsingEntity("MapEdges");

        entity.HasMany(m => m.Nodes)
            .WithMany(n => n.Maps)
            .UsingEntity("MapNodes");

        entity.HasMany(m => m.MapAgvs)
            .WithOne(ma => ma.Map)
            .HasForeignKey(ma => ma.MapId);

        entity.ConfigureAllProperties(except: new[] {nameof(Map.Nodes), nameof(Map.Edges), nameof(Map.MapAgvs)});

        EntityTypeBuilder<MapAgv> mapAgvEntity = model.Entity<MapAgv>();
        mapAgvEntity.HasKey(ma => new {ma.MapId, ma.AgvId});
        mapAgvEntity.ToTable("MapAgvs");
        mapAgvEntity.HasOne(ma => ma.Agv)
            .WithMany()
            .HasForeignKey(ma => ma.AgvId);
    }
}