using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.AgvSettings;

/// <inheritdoc />  
public class SettingEntityConfiguration : IEntityTypeConfiguration<Setting>
{
    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Description);
        // TODO restrict lenght of Value, ValueType
    }
}