using System.Text.Json;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class GraphNodeEntityConfiguration : IEntityTypeConfiguration<GraphNode>
{
    private const string TableName = "GraphNode";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GraphNode> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.NodeName);
        
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new JsonNodeTypeConverter());
        builder.Property(e => e.Type).HasConversion(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<ICollection<AgvGraph.NodeType>>(v, jsonOptions)!);
        builder.Property(e => e.TypeString)
            .UsePropertyAccessMode(PropertyAccessMode.Property)
            .HasMaxLength(DbStringLimits.Description)
            .HasComputedColumnSql("CASE WHEN LEFT([Type], 1) = '[' THEN [Type] ELSE JSON_VALUE([Type], '$') END");
        builder.Property(e => e.DockingDirection).HasMaxLength(DbStringLimits.State);

        builder.ConfigureAllProperties(except: [nameof(GraphNode.Lock)]);
    }
}