using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

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

        builder.ConfigureAllProperties(except: new[] { nameof(EfEdge.FromNodeId), nameof(EfEdge.ToNodeId), nameof(EfEdge.Maps), nameof(EfEdge.AgvGraphs) });

        builder.Property(e => e.VariantParameters).HasJsonConversion();
    }
}