using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.RestrictedArea;

/// <inheritdoc />  
public class RestrictedAreaPolygonPointEntityConfiguration : IEntityTypeConfiguration<RestrictedAreaPolygonPoint>
{
    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<RestrictedAreaPolygonPoint> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LocationId);
        builder.ToTable("RestrictedAreaPolygonPoints");
    }
}
