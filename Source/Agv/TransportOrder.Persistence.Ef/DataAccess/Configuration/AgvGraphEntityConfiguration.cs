using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvGraphEntityConfiguration : IEntityTypeConfiguration<AgvGraph>
{
    private const string TableName = "AgvGraph";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvGraph> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(g => g.AgvCorrelationId);
        builder.HasOne<Agv>()
            .WithMany()
            .HasForeignKey(e => e.AgvCorrelationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Edges)
            .WithMany()
            .UsingEntity("AgvGraphEdges");

        builder.HasMany(m => m.Nodes)
            .WithMany()
            .UsingEntity("AgvGraphNodes");

        builder.ConfigureAllProperties(except: [nameof(AgvGraph.Nodes), nameof(AgvGraph.Edges)]);
    }
}