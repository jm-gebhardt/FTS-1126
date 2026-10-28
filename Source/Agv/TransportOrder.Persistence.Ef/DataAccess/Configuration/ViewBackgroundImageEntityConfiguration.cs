using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc/>
public class ViewBackgroundImageEntityConfiguration : IEntityTypeConfiguration<ViewBackgroundImage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ViewBackgroundImage> builder)
    {
        builder.ToTable(nameof(ViewBackgroundImage));

        builder.HasKey(e => e.ViewId);
        builder.Property(e => e.ViewId).ValueGeneratedNever();

        builder.HasOne<View>()
            .WithOne()
            .HasForeignKey<ViewBackgroundImage>(e => e.ViewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}