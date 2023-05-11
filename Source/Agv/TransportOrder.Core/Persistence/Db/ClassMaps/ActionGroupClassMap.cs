using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps;

/// <summary>
/// <see cref="ActionGroup"/> Entity Framework Mapping
/// </summary>
public class ActionGroupClassMap : ClassMap<ActionGroup>
{
    private const int MaxStateLength = 50;

    /// <summary>
    /// <see cref="ActionGroup"/> Entity Framework Mapping
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<ActionGroup> entity, ModelBuilder model)
    {
        entity.ConfigureAllProperties();
        entity.HasKey(x => x.CorrelationId);
        entity.Property(e => e.State).HasMaxLength(MaxStateLength);
        entity.Property(x => x.Actions).HasJsonConversion();
    }
}