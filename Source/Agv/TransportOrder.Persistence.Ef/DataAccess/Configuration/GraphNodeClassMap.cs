using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="EfNode" /> Entity Framework Mapping
/// </summary>
public class GraphNodeClassMap : ClassMap<EfNode>
{
    /// <inheritdoc />
    public override string TableName => "GraphNode";

    /// <inheritdoc />
    protected override void Configure(EntityTypeBuilder<EfNode> entity, ModelBuilder model)
    {
        entity.HasKey(e => e.Id);

        entity.ConfigureAllProperties(except: new[] {nameof(EfNode.Maps), nameof(EfNode.AgvGraphs)});
    }
}