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
        builder.OwnsMany(e => e.ConfigurationParameters, o =>
        {
            o.ToTable("TriggerConfigurationParameters");
        });
        builder.Navigation(e => e.ConfigurationParameters).AutoInclude();
        builder.HasMany(e => e.Locations)
            .WithOne()
            .HasForeignKey(e => e.TriggerId);
        builder.Navigation(e => e.Locations).AutoInclude();
    }
}