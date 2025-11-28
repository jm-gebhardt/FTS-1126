using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess.Configuration;

/// <inheritdoc />
public class MqttEdgeMappingEntityConfiguration : IEntityTypeConfiguration<MqttEdgeMapping>
{
    private const string MqttEdgeTable = "DeviceCommunicationMqttEdge";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MqttEdgeMapping> builder)
    {
        builder.ToTable(MqttEdgeTable);

        builder.HasKey(e => e.InternalEdge);
        builder.Property(e => e.EdgeId).HasMaxLength(128); 
        builder.Property(e => e.StartId).IsRequired().HasMaxLength(128); 
        builder.Property(e => e.EndId).HasMaxLength(128); 
        builder.Property(e => e.Bidirectional);
        builder.Property(e => e.MaxVelocity);
        builder.Property(e => e.EdgeType).HasMaxLength(128); 
        builder.Property(e => e.ExitAngle);
        builder.Property(e => e.ExitDistance);
        builder.Property(e => e.Attributes).HasJsonConversion();
        builder.Property(e => e.ControlPoints).HasJsonConversion();
    }
}