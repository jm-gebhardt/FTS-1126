using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportOrder.Domain.Models.Graph;

namespace TransportOrder.Persistence.Ef.DataAccess.Configuration;

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