using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class NewNodeEntityConfiguration : IEntityTypeConfiguration<Node>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        builder.ToTable("New" + nameof(Node));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LevelId);
        builder.Property(e => e.X);
        builder.Property(e => e.Y);
        builder.Ignore(e => e.Children);

        // Edge has two references to Node (StartNode/EndNode), so the inverse collections must be paired
        // explicitly here - a plain HasMany() on both sides is ambiguous and produces extra shadow FKs.
        builder.HasMany(e => e.IncomingEdges)
            .WithOne(e => e.EndNode)
            .HasForeignKey(e => e.EndNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.OutgoingEdges)
            .WithOne(e => e.StartNode)
            .HasForeignKey(e => e.StartNodeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.Description).HasMaxLength(DbStringLimits.Description);
        builder.Ignore(e => e.RoutingNodes);
        builder.Ignore(e => e.RoutingEdges);
    }
}