using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc/>
public class ViewEntityConfiguration : IEntityTypeConfiguration<View>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<View> builder)
    {
        builder.ToTable(nameof(View));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.LevelId).HasMaxLength(DbStringLimits.Name);

        builder.Ignore(e => e.IsGenerated);

        builder.ComplexProperty(e => e.Area);
        builder.ComplexProperty(e => e.Background);
    }
}