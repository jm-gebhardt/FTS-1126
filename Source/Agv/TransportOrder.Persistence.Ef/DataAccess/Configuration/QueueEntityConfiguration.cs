using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class QueueEntityConfiguration : IEntityTypeConfiguration<Queue>
{
    /// <inheritdoc /> 
    public void Configure(EntityTypeBuilder<Queue> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Name)
            .IsRequired()
            .HasMaxLength(DbStringLimits.Description);

        builder.Ignore(e => e.Head);
        builder.Ignore(e => e.NonEntryNodes);
        builder.Ignore(e => e.Tail);
        builder.Ignore(e => e.Agvs);

        builder.OwnsMany(q => q.Nodes, nodeBuilder =>
        {
            nodeBuilder.WithOwner().HasForeignKey("QueueId");
            nodeBuilder.HasKey(n => n.NodeId);
            nodeBuilder.Property(n => n.NodeId).IsRequired().HasMaxLength(DbStringLimits.NodeId);
            nodeBuilder.Property(n => n.Position).IsRequired();
            nodeBuilder.Property(n => n.OccupiedAgv);
            nodeBuilder.Property(n => n.LastOccupiedDate);
            nodeBuilder.Property(n => n.LastReservedDate);
            nodeBuilder.Property(n => n.ReservedAgv);

            nodeBuilder.HasOne(n => n.Node)
                .WithMany()
                .HasForeignKey(n => n.NodeId)
                .OnDelete(DeleteBehavior.Restrict);

            nodeBuilder.Navigation(n => n.Node)
                .AutoInclude();
        });

        builder.Navigation(q => q.Nodes)
            .AutoInclude();
    }
}