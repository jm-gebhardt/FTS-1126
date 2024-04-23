using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvEntityConfiguration : IEntityTypeConfiguration<Agv>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Agv> builder)
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

                    // read only property, until further implementation.
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
        builder.Property(e => e.AgvSettingsGroup);
    }
}