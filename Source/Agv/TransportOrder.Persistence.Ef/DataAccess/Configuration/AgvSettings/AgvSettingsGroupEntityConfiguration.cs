using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration.AgvSettings;

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
            .HasForeignKey<Domain.Models.Settings.AgvSettings>(s => s.AgvGroupId);
        builder.Navigation(s => s.AgvSetting).AutoInclude();

        // Add a unique constraint to GroupType and Identifier
        builder.HasIndex(e => new { e.GroupType, e.Identifier }).IsUnique();
    }
}