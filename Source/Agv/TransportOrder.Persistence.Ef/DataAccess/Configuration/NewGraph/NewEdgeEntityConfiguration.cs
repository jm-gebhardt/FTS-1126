using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class NewEdgeEntityConfiguration : IEntityTypeConfiguration<Edge>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Edge> builder)
    {
        builder.ToTable("New" + nameof(Edge));
        builder.HasKey(e => e.Id);
        // StartNode/EndNode are configured from the Node side (see NewNodeEntityConfiguration), paired
        // with Node.IncomingEdges/OutgoingEdges - do not also configure HasOne() here, it would be ambiguous.
        builder.Property(e => e.Description).HasMaxLength(DbStringLimits.Description);
        builder.Ignore(e => e.Children);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);
    }
}