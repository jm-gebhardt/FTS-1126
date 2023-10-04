using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="ActionGroup"/> Entity Framework Mapping
/// </summary>
public class ActionGroupClassMap : ClassMap<ActionGroup>
{
    private const int MaxStateLength = 50;
    private const int MaxActionTypeLength = 50;

    /// <summary>
    /// <see cref="ActionGroup"/> Entity Framework Mapping
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<ActionGroup> entity, ModelBuilder model)
    {
        entity.HasKey(g => g.CorrelationId);
        entity.Property(g => g.State).HasMaxLength(MaxStateLength);
        entity.HasIndex(g => g.AssignedAgv);

        entity.OwnsMany(g => g.Actions, a =>
        {
            a.HasKey(o => o.CorrelationId);
            a.Property(e => e.Sequence);
            a.Property(e => e.Nodes).HasJsonConversion();
            a.Property(e => e.State).HasMaxLength(MaxStateLength);
            a.Property(e => e.ActionType).HasMaxLength(MaxActionTypeLength);
            a.HasIndex(e => e.TargetNodeId);
        });

        entity.Navigation(g => g.Actions).AutoInclude();
    }
}