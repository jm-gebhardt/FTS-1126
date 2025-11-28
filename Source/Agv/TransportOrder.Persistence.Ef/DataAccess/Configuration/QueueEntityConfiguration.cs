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
            // The IsRequired should be useless as the string is non-nullable.
            .IsRequired()
            .HasMaxLength(DbStringLimits.Description);

        builder.Ignore(e => e.Head);
        builder.Ignore(e => e.NonEntryNodes);
        builder.Ignore(e => e.Tail);
        builder.Ignore(e => e.Agvs);

        builder.Property(q => q.ConsideredAgvs).HasJsonConversion();
        builder.Ignore(e => e.QueueReservationChanged);

        builder.OwnsMany(q => q.Nodes, nodeBuilder =>
        {
            nodeBuilder.WithOwner().HasForeignKey("QueueId");
            // TODO: Add proper relation to GraphNode
            nodeBuilder.HasKey(n => n.NodeId);
            // TODO: IsRequired is not needed as it's not nullable and remove HasMaxLength
            nodeBuilder.Property(n => n.NodeId).IsRequired().HasMaxLength(DbStringLimits.NodeId);
            nodeBuilder.Property(n => n.Position).IsRequired();
            nodeBuilder.Property(n => n.OccupiedAgv);
            nodeBuilder.Ignore(n => n.OccupiedAgvChanged);
            nodeBuilder.Property(n => n.LastOccupiedDate);
            nodeBuilder.Property(n => n.LastReservedDate);
            nodeBuilder.Property(n => n.ReservedAgv);
            nodeBuilder.Ignore(n => n.ReservedAgvChanged);

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