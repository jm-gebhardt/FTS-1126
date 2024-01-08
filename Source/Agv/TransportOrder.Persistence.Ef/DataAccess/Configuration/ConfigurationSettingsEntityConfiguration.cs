using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class ConfigurationSettingsEntityConfiguration : IEntityTypeConfiguration<ConfigurationSetting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConfigurationSetting> builder)
    {
        builder.HasKey(p => p.Key);
        builder.Property(p => p.Value).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.ConfigurationType).HasMaxLength(500).IsRequired();
    }
}