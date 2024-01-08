using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

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

        builder.HasKey(e => e.CorrelationId);
        builder.Property(e => e.Type).HasMaxLength(MaxLength);
        builder.Property(e => e.IpV4Address).HasMaxLength(15);
        builder.Property(e => e.Sensors).HasJsonConversion();
    }
}