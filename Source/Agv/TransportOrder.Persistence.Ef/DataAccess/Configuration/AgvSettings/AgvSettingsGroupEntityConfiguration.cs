using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.AgvSettings;

/// <inheritdoc />  
public class AgvSettingsGroupEntityConfiguration: IEntityTypeConfiguration<AgvSettingsGroup>
{
    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<AgvSettingsGroup> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.GroupType);
        builder.HasOne(e => e.AgvSetting)
            .WithOne()
            // TODO: Rename AgvGroupId to AgvSettingsGroupId for clarity
            .HasForeignKey<Domain.Models.Settings.AgvSettings>(s => s.AgvGroupId);
        builder.Navigation(s => s.AgvSetting).AutoInclude();

        // Add a unique constraint to GroupType and Identifier
        builder.HasIndex(e => new { e.GroupType, e.Identifier }).IsUnique();
    }
}