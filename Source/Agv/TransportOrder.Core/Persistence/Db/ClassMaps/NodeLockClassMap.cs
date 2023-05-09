using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps
{
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
}