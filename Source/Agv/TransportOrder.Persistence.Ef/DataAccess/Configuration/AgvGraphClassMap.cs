using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="EfAgvGraph" /> Entity Framework Mapping
/// </summary>
public class AgvGraphClassMap : ClassMap<EfAgvGraph>
{
    /// <inheritdoc />
    public override string TableName => "AgvGraph";

    /// <inheritdoc />
    protected override void Configure(EntityTypeBuilder<EfAgvGraph> entity, ModelBuilder model)
    {
        entity.HasKey(g => g.CorrelationId);

        entity.HasMany(m => m.Edges)
            .WithMany(n => n.AgvGraphs)
            .UsingEntity("AgvGraphEdges");

        entity.HasMany(m => m.Nodes)
            .WithMany(n => n.AgvGraphs)
            .UsingEntity("AgvGraphNodes");

        entity.ConfigureAllProperties(except: new[] {nameof(EfAgvGraph.Nodes), nameof(EfAgvGraph.Edges)});
    }
}