using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvDataEntityConfiguration : IEntityTypeConfiguration<AgvData>
{
    private const string AgvDataTableName = "AgvData";
    private const int MaxLength = 64;

    /// <inheritdoc />>
    public void Configure(EntityTypeBuilder<AgvData> builder)
    {
        builder.ToTable(AgvDataTableName);
        builder.ConfigureAllProperties();

        builder.HasKey(e => e.AgvCorrelationId);
        builder.HasOne<Agv>()
            .WithMany()
            .HasForeignKey(e => e.AgvCorrelationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Type).HasMaxLength(MaxLength);
        builder.Property(e => e.SoftwareVersion).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Model).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.IpV4Address).HasMaxLength(15);
        builder.Property(e => e.Sensors).HasJsonConversion();
    }
}