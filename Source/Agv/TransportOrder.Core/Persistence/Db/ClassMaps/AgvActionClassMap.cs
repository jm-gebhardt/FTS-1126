using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps;

/// <summary>
/// <see cref="AgvAction"/> Entity Framework Mapping
/// </summary>
public class AgvActionClassMap : ClassMap<AgvAction>
{
    private const int MaxStateLength = 50;

    /// <summary>
    /// <see cref="AgvAction"/> Entity Framework Mapping
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<AgvAction> entity, ModelBuilder model)
    {
        entity.ConfigureAllProperties();
        entity.HasKey(o => o.CorrelationId);
        entity.Property(e => e.Predecessor).HasColumnOrder(2);
        entity.Property(e => e.Successor).HasColumnOrder(3);
        entity.Property(e => e.Sequence).HasColumnOrder(4);
        entity.Property(e => e.Nodes).HasJsonConversion();
        entity.Property(e => e.State).HasMaxLength(MaxStateLength);
        entity.Property(e => e.ActionType).HasMaxLength(MaxActionTypeLength);
    }

    private const int MaxActionTypeLength = 50;
}