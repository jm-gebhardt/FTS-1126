using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess.Configuration;

/// <inheritdoc/>
public class MqttNodeEntityConfiguration : IEntityTypeConfiguration<MqttNode>
{
    private const string MqttNodeTable = "DeviceCommunicationMqttNode";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MqttNode> builder)
    {
        builder.ToTable(MqttNodeTable);
        builder.HasKey(e => e.NodeId);
        builder.Property(e => e.NodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.X);
        builder.Property(e => e.Y);
        builder.Property(e => e.Z);
    }
}