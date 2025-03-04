using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess.Configuration;

/// <inheritdoc />
public class OrderIdMappingEntityConfiguration : IEntityTypeConfiguration<OrderIdMapping>
{
    private const string OrderMappingTable = "DeviceCommunicationKarisOrderMapping";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderIdMapping> builder)
    {
        builder.ToTable(OrderMappingTable);
        builder.HasKey(e => e.VehicleOrderId);
        builder.Property(e => e.InternalOrderId);
    }
}