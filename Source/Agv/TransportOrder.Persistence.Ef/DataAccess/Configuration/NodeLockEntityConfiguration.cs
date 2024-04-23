using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

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