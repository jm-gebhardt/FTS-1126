using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="AgvData"/> Entity Framework Mapping
/// </summary>
public class AgvDataClassMap : ClassMap<AgvData>
{
    private const string AgvDataTableName = "AgvData";
    private const int MaxLength = 64;

    /// <summary>
    /// <see cref="AgvData" /> entity configurations
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<AgvData> entity, ModelBuilder model)
    {
        entity.ToTable(AgvDataTableName);
        entity.ConfigureAllProperties();

        entity.HasKey(e => e.CorrelationId);
        entity.Property(e => e.Type).HasMaxLength(MaxLength);
        entity.Property(e => e.IpV4Address).HasMaxLength(15);
        entity.Property(e => e.Sensors).HasJsonConversion();
    }
}