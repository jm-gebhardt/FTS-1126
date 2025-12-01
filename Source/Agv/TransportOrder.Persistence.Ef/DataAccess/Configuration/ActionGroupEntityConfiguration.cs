using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
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
        builder.Property(e => e.ShouldEscapeQueue);
        builder.Property(e => e.Priority).HasConversion<int>().HasDefaultValue(Priority.Default);
        builder.Property(g => g.State).HasMaxLength(DbStringLimits.State);
        builder.HasIndex(g => g.AssignedAgv);
        builder.Property(e => e.IsQueued);
        builder.Property(e => e.AssignedAgv);
        builder.Property(e => e.DoNotPause);
        builder.Property(e => e.HandlingUnit).HasMaxLength(DbStringLimits.OrderId);
        builder.Property(e => e.OrderId).HasMaxLength(DbStringLimits.OrderId);

        // Reasons is just an information, we will never want to query it, so we can simply store it as json without introducing new tables
        builder.Property(g => g.Reasons).HasJsonConversion();

        // TODO: move to own entity and AgvActionConfiguration
        builder.OwnsMany(g => g.Actions, a =>
        {
            a.WithOwner().HasForeignKey("ParentId");
            a.HasKey(o => o.CorrelationId);
            a.Ignore(o => o.Duration);
            a.Ignore(o => o.IsIncomplete);
            a.Ignore(o => o.IsMoveToCoordinate);
            a.Property(e => e.Sequence);
            // TODO: Replace json by mapping table to GraphNode including a Index to ensure proper ordering
            a.Property(e => e.Nodes).HasJsonConversion();
            // TODO: Edges is currently persisted as json. This is ok, but we should have an explicit definition for that here.
            a.Property(e => e.State).HasMaxLength(DbStringLimits.State);
            a.Property(e => e.ActionType).HasMaxLength(DbStringLimits.ActionType);
            a.Property(e => e.AdditionalInfo).HasMaxLength(DbStringLimits.Description);
            // TODO: Seems like we need a domain model for ActionTarget (currently we are using ActionTarget from Common.Messages)
            // With that we can have a GraphNode (TO.Domain) as the TargetAction.Node
            // and remove the owned Common.Domain.GraphNode (which does not have any relation to the actual GraphTable right now)
            a.OwnsOne(e => e.Target, c =>
            {
                c.HasOne<GraphNode>(t => t.Node);
                //     , n =>
                // {
                //     n.Property(nn => nn.Id);
                //     n.Property(nn => nn.Name).HasMaxLength(DbStringLimits.NodeId);
                //     n.Ignore(nn => nn.TimeStamp);
                //     n.Ignore(nn => nn.Type);
                //     n.Ignore(nn => nn.X);
                //     n.Ignore(nn => nn.Y);
                //     n.Ignore(nn => nn.Z);
                //     n.Ignore(nn => nn.DropLiftHeight);
                //     n.Ignore(nn => nn.PickLiftHeight);
                //     n.Ignore(nn => nn.DockingDirection);
                //     n.Ignore(nn => nn.Theta);
                // });

                c.OwnsOne(t => t.Position, p =>
                {
                    p.Ignore(pp => pp.PositionInitialized);
                    p.Ignore(pp => pp.DeviationRange);
                });
            });
            a.Property(o => o.InfoToAgv).HasJsonConversion();
        });

        builder.Navigation(g => g.Actions).AutoInclude();
    }
}