using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <summary>
/// Base (TPH) configuration for the routing graph hierarchy: <see cref="RoutingNode"/>, <see cref="RoutingEdge"/> and <see cref="ActionRoutingEdge"/>
/// are stored in a single table so that <see cref="RoutingEdge.StartNode"/>/<see cref="RoutingEdge.EndNode"/> can be plain self-referencing
/// foreign keys instead of requiring a join across per-type tables.
/// </summary>
public class RoutingResourceEntityConfiguration : IEntityTypeConfiguration<RoutingResource>
{
    private const string TableName = nameof(RoutingResource);

    private const string Discriminator = $"{nameof(RoutingResource)}Type";

    /// <summary>
    /// Shadow property holding the id of the owning <see cref="GraphResource"/> (a <see cref="Node"/> or an <see cref="Edge"/>).
    /// Intentionally NOT mapped as a foreign key: Node and Edge are stored in separate tables (see <see cref="NewNodeEntityConfiguration"/>/
    /// <see cref="NewEdgeEntityConfiguration"/>), so a single FK column cannot target either one depending on <see cref="ParentType"/>.
    /// <see cref="RoutingResource.Parent"/>/<see cref="GraphResource.Children"/> must be resolved by GraphRepository via an explicit
    /// join on (<see cref="ParentId"/>, <see cref="ParentType"/>) instead.
    /// </summary>
    public const string ParentId = nameof(ParentId);

    /// <summary>Shadow property identifying which table <see cref="ParentId"/> refers to (<c>"Node"</c> or <c>"Edge"</c>).</summary>
    private const string ParentType = nameof(ParentType);

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RoutingResource> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);

        builder.HasDiscriminator<string>(Discriminator)
            .HasValue<RoutingNode>(nameof(RoutingNode))
            .HasValue<RoutingEdge>(nameof(RoutingEdge))
            .HasValue<ActionRoutingEdge>(nameof(ActionRoutingEdge));
        builder.Property(Discriminator).HasMaxLength(DbStringLimits.Enum);

        builder.Property<Guid>(ParentId);
        builder.Property<string>(ParentType).HasMaxLength(DbStringLimits.Enum);
        builder.Ignore(e => e.Parent);

        // Symmetric self-reference (if A conflicts with B, B conflicts with A) using a single navigation property
        // isn't expressible as a normal EF many-to-many (that needs a distinct navigation per side). Stored as plain
        // ids in RoutingResourceConflicts (see RoutingResourceConflictEntityConfiguration) and resolved by GraphRepository.
        builder.Ignore(e => e.ConflictingResources);

        builder.HasOne(e => e.AgvType)
            .WithMany()
            .HasForeignKey($"{nameof(AgvType)}Id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
