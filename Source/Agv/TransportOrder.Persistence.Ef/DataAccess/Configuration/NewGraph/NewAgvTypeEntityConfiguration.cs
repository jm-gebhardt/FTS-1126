using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class NewAgvTypeEntityConfiguration : IEntityTypeConfiguration<AgvType>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AgvType> builder)
    {
        builder.ToTable("New" + nameof(AgvType));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.AgvTypeIdentifier).HasMaxLength(DbStringLimits.Description);
    }
}
