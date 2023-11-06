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
    public override string TableName => "AgvGraphEdge";

    /// <inheritdoc />
    protected override void Configure(EntityTypeBuilder<EfEdge> entity, ModelBuilder model)
    {
        entity.HasKey(nameof(EfEdge.GraphCorrelationId), nameof(EfEdge.Id));

        entity.HasOne<EfNode>()
            .WithMany()
            .HasForeignKey(nameof(EfEdge.GraphCorrelationId), nameof(EfEdge.FromNodeId))
            .OnDelete(DeleteBehavior.ClientCascade);

        entity.HasOne<EfNode>()
            .WithMany()
            .HasForeignKey(nameof(EfEdge.GraphCorrelationId), nameof(EfEdge.ToNodeId))
            .OnDelete(DeleteBehavior.ClientCascade);

        entity.ConfigureAllProperties();

        entity.Property(e => e.VariantParameters).HasJsonConversion();
    }
}