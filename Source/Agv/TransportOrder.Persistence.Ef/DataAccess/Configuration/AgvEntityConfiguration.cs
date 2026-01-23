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
        builder.Property(e => e.SerialNumber).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Description);
        builder.ConfigureAllProperties(p =>
            p.CanWrite &&
            p.PropertyType != typeof(AgvStatus) &&
            p.PropertyType != typeof(AgvMode) &&
            p.Name != nameof(Agv.Name) &&
            p.Name != nameof(Agv.SerialNumber));
        builder.OwnsOne(e => e.Mode, o =>
        {
            o.WithOwner().HasForeignKey(e => e.AgvCorrelationId);
            o.Property(e => e.ReasonDescription).HasMaxLength(DbStringLimits.Description);
            o.Property(e => e.InitiatingUser).HasMaxLength(DbStringLimits.User);
        });
        builder.Navigation(e => e.Mode).AutoInclude();
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

                    case nameof(AgvStatus.OrderId):
                        o.Property(propertyInfo.PropertyType, propertyInfo.Name).HasMaxLength(DbStringLimits.OrderId);
                        break;

                    case nameof(AgvStatus.ZoneIds):
                        o.Property(s => s.ZoneIds)
                            .HasConversion(EfValueConverters.CreateJsonConverter<ICollection<string>?>())
                            .Metadata.SetValueComparer(EfValueConverters.CreateListComparer<string>());
                        break;

                    case nameof(AgvStatus.StationIds):
                        o.Property(s => s.StationIds)
                            .HasConversion(EfValueConverters.CreateJsonConverter<ICollection<string>?>())
                            .Metadata.SetValueComparer(EfValueConverters.CreateListComparer<string>());
                        break;

                    default:
                        o.Property(propertyInfo.PropertyType, propertyInfo.Name);
                        break;
                }
            }
        });
        builder.Navigation(e => e.AgvStatus).AutoInclude();
        builder.HasOne<Domain.Models.Settings.AgvSettingsGroup>()
            .WithMany()
            .HasForeignKey(e => e.AgvSettingsGroup)
            .OnDelete(DeleteBehavior.Restrict);
    }
}