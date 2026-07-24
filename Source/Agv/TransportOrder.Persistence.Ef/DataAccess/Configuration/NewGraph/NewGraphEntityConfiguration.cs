using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc />  
public class NewGraphEntityConfiguration : IEntityTypeConfiguration<Graph>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Graph> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasMany(e => e.Edges);

        builder.HasMany(e => e.Nodes);

        builder.HasMany(e => e.Stations);
    }
}