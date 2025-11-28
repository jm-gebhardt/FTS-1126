using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class GraphEdgeEntityConfiguration : IEntityTypeConfiguration<EfEdge>
{
    private const string TableName = "GraphEdge";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EfEdge> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);

        builder.HasOne<EfNode>()
            .WithMany()
            .HasForeignKey(e => e.FromNodeId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne<EfNode>()
            .WithMany()
            .HasForeignKey(e => e.ToNodeId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.ConfigureAllProperties(except: [nameof(EfEdge.FromNodeId), nameof(EfEdge.ToNodeId), nameof(EfEdge.Maps), nameof(EfEdge.AgvGraphs)]);

        builder.Property(e => e.VariantParameters).HasJsonConversion();
    }
}