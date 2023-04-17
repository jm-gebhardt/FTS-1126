using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;

/// <summary>
/// <see cref="AgvAction"/> Entity Framework Mapping
/// </summary>
public class AgvActionClassMap : SagaClassMap<AgvAction>
{
    private const string MapTableName = "AgvAction";
    private const int MaxStateLength = 50;
    private const int MaxActionTypeLength = 50;

    /// <summary>
    /// <see cref="AgvAction"/> Entity Framework Mapping
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<AgvAction> entity, ModelBuilder model)
    {
        // set table names
        entity.ToTable(MapTableName);
        entity.HasKey(o => o.CorrelationId);
        entity.ConfigureAllProperties();
        entity.Property(e => e.Nodes).HasJsonConversion();
        entity.Property(e => e.State).HasMaxLength(MaxStateLength);
        entity.Property(e => e.ActionType).HasMaxLength(MaxActionTypeLength);
    }
}