using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvGraphEntityConfiguration : IEntityTypeConfiguration<EfAgvGraph>
{
    private const string TableName = "AgvGraph";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EfAgvGraph> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(g => g.CorrelationId);

        builder.HasMany(m => m.Edges)
            .WithMany(n => n.AgvGraphs)
            .UsingEntity("AgvGraphEdges");

        builder.HasMany(m => m.Nodes)
            .WithMany(n => n.AgvGraphs)
            .UsingEntity(j =>
            {
                j.ToTable("AgvGraphNodes");
                j.Property<string>("NodesId").IsCaseSensitive();
            });

        builder.ConfigureAllProperties(except: new[] { nameof(EfAgvGraph.Nodes), nameof(EfAgvGraph.Edges) });
    }
}