using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class GraphEdgeEntityConfiguration : IEntityTypeConfiguration<GraphEdge>
{
    private const string TableName = "GraphEdge";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GraphEdge> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);

        builder.HasOne<GraphNode>()
            .WithMany()
            .HasForeignKey(e => e.FromNodeId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne<GraphNode>()
            .WithMany()
            .HasForeignKey(e => e.ToNodeId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.ConfigureAllProperties(except: [nameof(GraphEdge.FromNodeId), nameof(GraphEdge.ToNodeId)]);

        builder.Property(e => e.VariantParameters).HasJsonConversion();
    }
}