using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class NodeLockEntityConfiguration : IEntityTypeConfiguration<NodeLock>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NodeLock> builder)
    {
        builder.HasKey(x => x.NodeId);
        builder.HasOne<GraphNode>()
            .WithMany()
            .HasForeignKey(e => e.NodeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAllProperties();
    }
}