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

        builder.OwnsMany(q => q.Nodes, nodeBuilder =>
        {
            nodeBuilder.WithOwner().HasForeignKey("QueueId");
            nodeBuilder.Property(n => n.NodeId).IsRequired().HasMaxLength(DbStringLimits.NodeId);
            nodeBuilder.Property(n => n.Position).IsRequired();

            nodeBuilder.HasOne(n => n.Node)
                .WithMany()
                .HasForeignKey(n => n.NodeId)
                .OnDelete(DeleteBehavior.Restrict);

            nodeBuilder.Navigation(n => n.Node)
                .AutoInclude();

            nodeBuilder.HasOne(n => n.State)
                .WithMany()
                .HasForeignKey(n => n.NodeId)
                .OnDelete(DeleteBehavior.Restrict);

            nodeBuilder.Navigation(n => n.State)
                .AutoInclude();
        });

        builder.Navigation(q => q.Nodes)
            .AutoInclude();
    }
}