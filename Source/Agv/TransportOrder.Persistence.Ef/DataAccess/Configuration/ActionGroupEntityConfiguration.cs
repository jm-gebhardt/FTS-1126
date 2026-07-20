using Gebhardt.Acc.Common.Domain.Models;
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
        builder.Ignore(e => e.TotalDuration);
        builder.Ignore(e => e.IsIncomplete);
        builder.Ignore(e => e.IsQueueingOrder);
        builder.Ignore(e => e.LogEntries);
        builder.Ignore(e => e.IsRegisteredForDoppelspielDrop);
        builder.Ignore(e => e.HasQueuedDoppelspielpartner);
        builder.Property(e => e.ShouldEscapeQueue);
        builder.Property(e => e.Priority).HasConversion<int>().HasDefaultValue(Priority.Default);
        builder.Property(g => g.State).HasMaxLength(DbStringLimits.State);
        builder.HasIndex(g => g.AssignedAgv);
        builder.Property(e => e.Duration);
        builder.Property(e => e.IsQueued);
        builder.Property(e => e.AssignedAgv);
        builder.Property(e => e.DoNotPause);
        builder.Property(e => e.HandlingUnit).HasMaxLength(DbStringLimits.OrderId);
        builder.Property(e => e.OrderId).HasMaxLength(DbStringLimits.OrderId);
        builder.Property(e => e.CanRunDoppelspiel);
        builder.Property(e => e.ConsideredForDoppelspielDate);
        builder.HasIndex(e => e.CreatedDate);

        // Reasons is just an information, we will never want to query it, so we can simply store it as json without introducing new tables
        builder.Property(g => g.Reasons).HasJsonConversion();

        builder.OwnsMany(g => g.Actions, a =>
        {
            a.WithOwner().HasForeignKey("ParentId");
            a.HasKey(o => o.CorrelationId);
            a.Ignore(o => o.Duration);
            a.Ignore(o => o.IsIncomplete);
            a.Ignore(o => o.IsMoveToCoordinate);
            a.Property(e => e.Sequence);
            a.Ignore(o => o.Nodes);

            a.OwnsMany(x => x.AgvActionNodes, n =>
            {
                n.ToTable("AgvActionNodes");
                n.HasKey(x => x.Id);

                n.Property(x => x.Index);
                n.Property(x => x.CreatedDate);

                n.HasOne(x => x.Node)
                    .WithMany()
                    .HasForeignKey(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                n.Navigation(x => x.Node).AutoInclude();
            });
            a.Navigation(o => o.AgvActionNodes).AutoInclude();

            a.Property(e => e.Edges).HasJsonConversion();
            a.Property(e => e.IsRegisteredForDoppelspiel);
            a.Property(e => e.DoppelspielPartner);
            a.Property(e => e.State).HasMaxLength(DbStringLimits.State);
            a.Property(e => e.ActionType).HasMaxLength(DbStringLimits.ActionType);
            a.Property(e => e.AdditionalInfo).HasMaxLength(DbStringLimits.Description);

            a.OwnsOne(e => e.Target, c =>
            {
                c.HasOne(t => t.Node)
                    .WithMany()
                    .HasForeignKey(t => t.NodeId)
                    .OnDelete(DeleteBehavior.Restrict);
                c.Property(t => t.DoppelspielMode);
                c.Property(t => t.StationOptions).HasJsonConversion();
                c.OwnsOne(t => t.Position, p =>
                {
                    p.Ignore(pp => pp.PositionInitialized);
                    p.Ignore(pp => pp.DeviationRange);
                });
                
                c.Navigation(t => t.Node).AutoInclude();
            });
            a.Property(o => o.InfoToAgv).HasJsonConversion();
        });

        builder.Navigation(g => g.Actions).AutoInclude();
    }
}
