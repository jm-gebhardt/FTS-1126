using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class NewEdgeEntityConfiguration : IEntityTypeConfiguration<Edge>
{
    /// <summary>
    /// The name of the Edge ID foreign key property of the RoutingResources.
    /// </summary>
    public const string EdgeId = "EdgeId";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Edge> builder)
    {
        builder.ToTable("New" + nameof(Edge));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.ShortName).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);

        builder.HasOne<Node>()
            .WithMany()
            .HasForeignKey(e => e.StartNodeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne<Node>()
            .WithMany()
            .HasForeignKey(e => e.EndNodeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasMany(e => e.RoutingEdges)
            .WithOne()
            .HasForeignKey(EdgeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}