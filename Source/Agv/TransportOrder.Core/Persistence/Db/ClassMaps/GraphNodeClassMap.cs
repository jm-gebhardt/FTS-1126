using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps;

/// <summary>
/// <see cref="EfNode" /> Entity Framework Mapping
/// </summary>
public class GraphNodeClassMap : ClassMap<EfNode>
{
    /// <inheritdoc />
    public override string TableName => "AgvGraphNode";

    /// <inheritdoc />
    protected override void Configure(EntityTypeBuilder<EfNode> entity, ModelBuilder model)
    {
        entity.HasKey(nameof(EfNode.GraphCorrelationId), nameof(EfNode.Id));
    }
}