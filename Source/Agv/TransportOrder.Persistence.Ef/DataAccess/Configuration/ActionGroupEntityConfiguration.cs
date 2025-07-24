using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class ActionGroupEntityConfiguration : IEntityTypeConfiguration<ActionGroup>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ActionGroup> builder)
    {
        builder.HasKey(g => g.CorrelationId);
        builder.Ignore(e => e.FirstNotStartedAction);
        builder.Ignore(e => e.IsMovingCargo);
        builder.Ignore(e => e.IsChargingOrder);
        builder.Ignore(e => e.IsIncomplete);
        builder.Ignore(e => e.Duration);
        builder.Ignore(e => e.IsQueueingOrder);
        builder.Property(g => g.State).HasMaxLength(DbStringLimits.State);
        builder.HasIndex(g => g.AssignedAgv);
        builder.Property(e => e.IsQueued);
        builder.Property(e => e.AssignedAgv);
        builder.Property(e => e.HandlingUnit).HasMaxLength(DbStringLimits.OrderId);
        builder.Property(e => e.OrderId).HasMaxLength(DbStringLimits.OrderId);

        // Reasons is just an information, we will never want to query it, so we can simply store it as json without introducing new tables
        builder.Property(g => g.Reasons).HasJsonConversion();
        builder.OwnsMany(g => g.Actions, a =>
        {
            a.HasKey(o => o.CorrelationId);
            a.Ignore(o => o.Duration);
            a.Ignore(o => o.IsIncomplete);
            a.Ignore(o => o.IsMoveToCoordinate);
            a.Property(e => e.Sequence);
            a.Property(e => e.Nodes).HasJsonConversion();
            a.Property(e => e.State).HasMaxLength(DbStringLimits.State);
            a.Property(e => e.ActionType).HasMaxLength(DbStringLimits.ActionType);
            a.Property(e => e.AdditionalInfo).HasMaxLength(DbStringLimits.Description);
            a.OwnsOne(e => e.Target, c =>
            {
                c.Property(t => t.NodeId).HasMaxLength(DbStringLimits.NodeId);
                c.OwnsOne(t => t.Position);
            });
            a.Property(o => o.InfoToAgv).HasJsonConversion();
        });

        builder.Navigation(g => g.Actions).AutoInclude();
    }
}