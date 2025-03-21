using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Zone;

/// <inheritdoc />  
public class ZonePolygonPointEntityConfiguration: IEntityTypeConfiguration<ZonePolygonPoint>
{
    private const string TableName = "AgvZonePolygonPoints";
    
    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<ZonePolygonPoint> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LocationId);
        builder.ToTable(TableName);
    }
}