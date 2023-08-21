using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps;

/// <summary>
/// 
/// </summary>
public class ConfigurationSettingsClassMap : ClassMap<ConfigurationSetting>
{
    /// <inheritdoc />
    protected override void Configure(EntityTypeBuilder<ConfigurationSetting> entity, ModelBuilder model)
    {
        entity.HasKey(p => p.Key);
        entity.Property(p => p.Value).HasMaxLength(1000).IsRequired();
        entity.Property(p => p.ConfigurationType).HasMaxLength(500).IsRequired();
    }
        
}