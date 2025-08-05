using System.Text.Json;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class GraphNodeEntityConfiguration : IEntityTypeConfiguration<EfNode>
{
    private const string TableName = "GraphNode";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EfNode> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(DbStringLimits.NodeId).IsCaseSensitive();
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new JsonNodeTypeConverter());
        builder.Property(e => e.Type).HasConversion(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<object>(v, jsonOptions)!);
        builder.Property(e => e.DockingDirection).HasMaxLength(DbStringLimits.State);

        builder.ConfigureAllProperties(except: new[] { nameof(EfNode.Maps), nameof(EfNode.AgvGraphs) });
    }
}