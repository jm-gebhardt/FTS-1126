using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class GraphNodeEntityConfiguration : IEntityTypeConfiguration<EfNode>
{
    private const string TableName = "GraphNode";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EfNode> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);

        builder.ConfigureAllProperties(except: new[] {nameof(EfNode.Maps), nameof(EfNode.AgvGraphs)});
    }
}