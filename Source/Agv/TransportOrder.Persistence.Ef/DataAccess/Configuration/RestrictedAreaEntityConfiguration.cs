using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class RestrictedAreaEntityConfiguration: IEntityTypeConfiguration<RestrictedArea>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RestrictedArea> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("RestrictedAreas");
        builder.HasMany(l => l.Area)
            .WithOne()
            .HasForeignKey(p => p.LocationId);
        builder.Navigation(n => n.Area).AutoInclude();
    }
}