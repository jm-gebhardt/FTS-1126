using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class ActionRoutingEdgeEntityConfiguration : IEntityTypeConfiguration<ActionRoutingEdge>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ActionRoutingEdge> builder)
    {
        builder.Property(e => e.ActionType);
    }
}
