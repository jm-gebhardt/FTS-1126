using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Trigger;

/// <inheritdoc />  
public class TriggerPolygonEntityConfiguration : IEntityTypeConfiguration<TriggerPolygonPoint>
{
    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<TriggerPolygonPoint> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LocationId);
        builder.ToTable("TriggerPolygonPoints");
    }
}