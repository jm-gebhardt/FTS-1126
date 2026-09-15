using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class RoutingNodeEntityConfiguration : IEntityTypeConfiguration<RoutingNode>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RoutingNode> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasOne<AgvType>()
            .WithMany()
            .HasForeignKey(e => e.AgvTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Theta);

        builder.HasOne<LoadState>()
            .WithMany()
            .HasForeignKey(e => e.LoadStateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
