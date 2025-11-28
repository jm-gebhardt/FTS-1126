using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Trigger;

/// <inheritdoc />  
public class TriggerEntityConfiguration : IEntityTypeConfiguration<Domain.Models.Trigger.Trigger>
{
    /// <inheritdoc /> 
    public void Configure(EntityTypeBuilder<Domain.Models.Trigger.Trigger> builder)
    {
        builder.HasKey(e => e.Id);
        // TODO: Restrict lenght of: Urn, Version, Description
        builder.Property(e => e.DisplayName).HasMaxLength(DbStringLimits.Description);
        builder.OwnsMany(e => e.ConfigurationParameters, o =>
        {
            o.ToTable("DefaultTriggerConfigurationParameters");
            // TODO: Restrict length of Description, Value, Validation
            o.Property(e => e.Name).HasMaxLength(DbStringLimits.Description);
        });
        builder.Navigation(e => e.ConfigurationParameters).AutoInclude();
        
        builder.HasMany(e => e.Locations)
            .WithOne()
            .HasForeignKey(e => e.TriggerId);
        builder.Navigation(e => e.Locations).AutoInclude();
    }
}