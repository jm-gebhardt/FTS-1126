using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TriggerService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class TriggerEntityConfiguration : IEntityTypeConfiguration<Trigger>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Trigger> builder)
    {
        builder.ToTable("TriggerServiceTriggers");
        builder.OwnsMany<ConfigurationParameter>(e => e.ConfigurationParameters, o =>
        {
            o.ToTable("TriggerServiceConfigurationParameters");
        });
        builder.Navigation(e => e.ConfigurationParameters).AutoInclude();
    }
}