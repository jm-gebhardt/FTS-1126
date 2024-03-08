using Gebhardt.StoreWare.Agv.Common.Persistence.Ef;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class NodeLockEntityConfiguration : IEntityTypeConfiguration<NodeLock>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NodeLock> builder)
    {
        builder.HasKey(x => x.NodeId);
        builder.ConfigureAllProperties();
    }
}