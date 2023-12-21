using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="EfEdge" /> Entity Framework Mapping
/// </summary>
public class GraphEdgeClassMap : ClassMap<EfEdge>
{
    /// <inheritdoc />
    public override string TableName => "GraphEdge";

    /// <inheritdoc />
    protected override void Configure(EntityTypeBuilder<EfEdge> entity, ModelBuilder model)
    {
        entity.HasKey(e => e.Id);

        entity.HasOne<EfNode>()
            .WithMany()
            .HasForeignKey(e => e.FromNodeId)
            .OnDelete(DeleteBehavior.ClientCascade);

        entity.HasOne<EfNode>()
            .WithMany()
            .HasForeignKey(e => e.ToNodeId)
            .OnDelete(DeleteBehavior.ClientCascade);

        entity.ConfigureAllProperties(except: new[] {nameof(EfEdge.FromNodeId), nameof(EfEdge.ToNodeId), nameof(EfEdge.Maps), nameof(EfEdge.AgvGraphs)});

        entity.Property(e => e.VariantParameters).HasJsonConversion();
    }
}