using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.Extensions
{
    /// <summary>
    /// ModelBuilder extension 
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="ConfigurationSetting"/> entity
        /// </summary>
        /// <param name="modelBuilder"></param>
        public static void BuildConfigurationSetting(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConfigurationSetting>().HasKey(p => p.Key);
            modelBuilder.Entity<ConfigurationSetting>().Property(p => p.Value).HasMaxLength(1000).IsRequired();
            modelBuilder.Entity<ConfigurationSetting>().Property(p => p.ConfigurationType).HasMaxLength(500).IsRequired();
            modelBuilder.Entity<ConfigurationSetting>().ToTable(ConfigurationSetting.TableName);
        }
    }
}