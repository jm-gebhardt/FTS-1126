using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvEntityConfiguration : IEntityTypeConfiguration<Domain.Models.Agv.Agv>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Domain.Models.Agv.Agv> builder)
    {
        builder.HasKey(g => g.CorrelationId);

        builder.ConfigureAllProperties(p => p.CanWrite && p.PropertyType != typeof(AgvStatus));

        builder.OwnsOne(e => e.AgvStatus, o =>
        {
            o.ToTable(nameof(AgvStatus));
            o.WithOwner().HasForeignKey(s => s.CorrelationId);
            o.HasKey(s => s.CorrelationId);
            foreach (var propertyInfo in typeof(AgvStatus).GetProperties())
            {
                switch (propertyInfo.Name)
                {
                    case nameof(AgvStatus.CorrelationId):
                        continue;

                    // read only properties, until further implementation.
                    case nameof(AgvStatus.EstimatedFullyChargedTime):
                    case nameof(AgvStatus.LastBatteryReplacement):
                        o.Ignore(propertyInfo.Name);
                        break;

                    default:
                        o.Property(propertyInfo.PropertyType, propertyInfo.Name);
                        break;
                }
            }
        });
        builder.Navigation(e => e.AgvStatus).AutoInclude();
    }
}