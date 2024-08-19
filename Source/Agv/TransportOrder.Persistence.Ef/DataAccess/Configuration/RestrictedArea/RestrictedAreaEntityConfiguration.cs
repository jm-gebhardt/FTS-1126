using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.RestrictedArea;

/// <inheritdoc />
public class RestrictedAreaEntityConfiguration : IEntityTypeConfiguration<Domain.Models.RestrictedArea.RestrictedArea>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Domain.Models.RestrictedArea.RestrictedArea> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("RestrictedAreas");
        builder.HasMany(e => e.Area)
            .WithOne()
            .HasForeignKey(p => p.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Area).AutoInclude();
    }
}