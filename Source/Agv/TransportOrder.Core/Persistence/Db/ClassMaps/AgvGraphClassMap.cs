using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps;

/// <summary>
/// <see cref="EfGraph" /> Entity Framework Mapping
/// </summary>
public class AgvGraphClassMap : ClassMap<EfGraph>
{
    /// <inheritdoc />
    public override string TableName => "AgvGraph";

    /// <inheritdoc />
    protected override void Configure(EntityTypeBuilder<EfGraph> entity, ModelBuilder model)
    {
        entity.HasKey(g => g.CorrelationId);

        entity.HasMany(g => g.Nodes)
            .WithOne()
            .HasForeignKey(a => a.GraphCorrelationId)
            .HasPrincipalKey(g => g.CorrelationId)
            .OnDelete(DeleteBehavior.ClientCascade);

        entity.HasMany(g => g.Edges)
            .WithOne()
            .HasForeignKey(a => a.GraphCorrelationId)
            .HasPrincipalKey(g => g.CorrelationId)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}