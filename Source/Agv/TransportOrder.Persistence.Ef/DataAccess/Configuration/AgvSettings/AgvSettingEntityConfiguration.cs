using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.AgvSettings;

/// <inheritdoc />  
public class AgvSettingEntityConfiguration: IEntityTypeConfiguration<Domain.Models.Settings.AgvSettings>
{
    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<Domain.Models.Settings.AgvSettings> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AgvGroupId);
        builder.HasOne(e => e.AgvOfflineTimeoutMsWhileIdle)
            .WithMany()
            .HasForeignKey(e => e.AgvOfflineTimeoutMsWhileIdleId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(e => e.AgvOfflineTimeoutMsWhileIdle).AutoInclude();
        builder.HasOne(e => e.AgvOfflineTimeoutMsWhileBusy)
            .WithMany()
            .HasForeignKey(e => e.AgvOfflineTimeoutMsWhileBusyId)
            .OnDelete(DeleteBehavior.NoAction); 
        builder.Navigation(e => e.AgvOfflineTimeoutMsWhileBusy).AutoInclude();
        builder.HasOne(e => e.BatteryLevelCritical)
            .WithMany()
            .HasForeignKey(e => e.BatteryLevelCriticalId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(e => e.BatteryLevelCritical).AutoInclude();
        builder.HasOne(e => e.BatteryLevelLow)
            .WithMany()
            .HasForeignKey(e => e.BatteryLevelLowId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(e => e.BatteryLevelLow).AutoInclude();
        builder.HasOne(e => e.MinChargeUp)
            .WithMany()
            .HasForeignKey(e => e.MinChargeUpId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(e => e.MinChargeUp).AutoInclude();
        builder.HasOne(e => e.MaxChargeUp)
            .WithMany()
            .HasForeignKey(e => e.MaxChargeUpId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(e => e.MaxChargeUp).AutoInclude();
        builder.HasOne(e => e.InfiniteCharging)
            .WithMany()
            .HasForeignKey(e => e.InfiniteChargingId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(e => e.InfiniteCharging).AutoInclude();
    }
}