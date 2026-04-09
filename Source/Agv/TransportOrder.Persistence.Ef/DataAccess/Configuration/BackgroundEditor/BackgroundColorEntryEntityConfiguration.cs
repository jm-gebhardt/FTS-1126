using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.BackgroundEditor;

/// <inheritdoc />
public class BackgroundColorEntryEntityConfiguration : IEntityTypeConfiguration<BackgroundColorEntry>
{
    private const string TableName = "BackgroundColorEntries";
   
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BackgroundColorEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable(TableName);

        builder.Property(e => e.BackgroundId).IsRequired();
        builder.Property(e => e.PaletteType).HasConversion<string>().IsRequired();
        builder.Property(e => e.Value).IsRequired();

        builder.HasIndex(e => new { e.BackgroundId, e.PaletteType });
    }
}
