using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// Entity configuration for <see cref="AgvGraph"/>.
/// </summary>
public class AgvGraphEntityConfiguration : IEntityTypeConfiguration<AgvGraph>
{
    private const string TableName = "DeviceCommunicationVda5050AgvGraph";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AgvGraph> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.AgvCorrelationId);
        builder.HasMany(e => e.Nodes)
            .WithOne()
            .HasForeignKey(nameof(AgvGraph.AgvCorrelationId))
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Edges)
            .WithOne()
            .HasForeignKey(nameof(AgvGraph.AgvCorrelationId))
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Nodes).AutoInclude();
        builder.Navigation(e => e.Edges).AutoInclude();
    }
}