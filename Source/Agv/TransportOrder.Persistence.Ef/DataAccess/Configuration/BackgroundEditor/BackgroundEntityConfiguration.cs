using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.BackgroundEditor;

/// <inheritdoc />
public class BackgroundEntityConfiguration : IEntityTypeConfiguration<Background>
{
    private const string TableName = "Backgrounds"; 

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Background> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable(TableName);

        builder.Property(e => e.MapId);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);

        builder.HasMany(e => e.Objects)
            .WithOne()
            .HasForeignKey(o => o.BackgroundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Layers)
            .WithOne()
            .HasForeignKey(l => l.BackgroundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ColorEntries)
            .WithOne()
            .HasForeignKey(c => c.BackgroundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Objects).AutoInclude();
        builder.Navigation(e => e.Layers).AutoInclude();
        builder.Navigation(e => e.ColorEntries).AutoInclude();
    }
}
