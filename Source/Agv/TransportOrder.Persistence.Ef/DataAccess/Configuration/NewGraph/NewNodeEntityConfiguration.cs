using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class NewNodeEntityConfiguration : IEntityTypeConfiguration<Node>
{
    /// <summary>
    /// The name of the node ID foreign key property of the RoutingResources.
    /// </summary>
    public const string NodeId = "NodeId";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        builder.ToTable("New" + nameof(Node));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.ShortName).HasMaxLength(DbStringLimits.Description);

        builder.Property(e => e.X);
        builder.Property(e => e.Y);
        builder.Property(e => e.LevelId).HasMaxLength(DbStringLimits.Name);

        builder.HasMany(e => e.RoutingNodes)
            .WithOne()
            .HasForeignKey(NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.RoutingEdges)
            .WithOne()
            .HasForeignKey(NodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}