using Gebhardt.StoreWare.Agv.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportOrder.Domain.Models.Node;

namespace TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="NodeLock"/> Entity Framework Mapping
/// </summary>
public class NodeLockClassMap : ClassMap<NodeLock>
{
    /// <inheritdoc />
    protected override void Configure(EntityTypeBuilder<NodeLock> entity, ModelBuilder model)
    {
        entity.HasKey(x => x.NodeId);
        entity.ConfigureAllProperties();
    }
}