using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.AgvSettings;

/// <inheritdoc />  
public class FactoryTimeEntityConfiguration : IEntityTypeConfiguration<FactoryTime>
{
    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<FactoryTime> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SerializedFactoryTimeZone).HasMaxLength(DbStringLimits.Description);
        builder.Ignore(e => e.FactoryTimeZone);
        builder.Ignore(e => e.Dirty);
    }
}