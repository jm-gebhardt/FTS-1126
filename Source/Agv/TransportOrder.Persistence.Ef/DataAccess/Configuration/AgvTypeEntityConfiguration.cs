using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvTypeEntityConfiguration : IEntityTypeConfiguration<AgvType>
{
    private const string AgvTypeTableName = "AgvType";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvType> builder)
    {
        builder.ToTable(AgvTypeTableName);
        builder.ConfigureAllProperties();

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.AgvTypeIdentifier).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Manufacturer).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.SeriesName).HasMaxLength(DbStringLimits.Description);
        builder.HasIndex(e => e.AgvTypeIdentifier).IsUnique();
    }
}