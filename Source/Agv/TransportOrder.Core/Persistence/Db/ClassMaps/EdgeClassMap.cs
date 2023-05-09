using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps;

/// <summary>
/// Edges class map
/// </summary>
public class EdgeClassMap: ClassMap<Edge>
{
    /// <summary>
    /// Edge configuration
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<Edge> entity, ModelBuilder model)
    {
        entity.HasKey(nameof(Edge.FromId), nameof(Edge.ToId));

        entity.HasOne(d => d.From)
            .WithMany(p => p.EdgesFrom)
            .HasForeignKey(d => d.FromId)
            .OnDelete(DeleteBehavior.ClientNoAction);

        entity.HasOne(d => d.To)
            .WithMany(p => p.EdgesTo)
            .HasForeignKey(d => d.ToId)
            .OnDelete(DeleteBehavior.ClientNoAction);
    }
}