using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// Entity configuration for <see cref="Node"/>.
/// </summary>
public class NodeEntityConfiguration : IEntityTypeConfiguration<Node>
{
    private const string TableName = "DeviceCommunicationVda5050Node";
    private const string ShadowId = "Id";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.NodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.MapId).HasMaxLength(DbStringLimits.NodeName);
        builder.OwnsOne(e => e.Position);
        builder.OwnsMany(e => e.AgvActions, actionBuilder =>
        {
            actionBuilder.ToTable(TableName + nameof(AgvNodeAction));
            actionBuilder.Property(a => a.ActionType).HasMaxLength(DbStringLimits.Name);
            actionBuilder.OwnsMany(a => a.Parameters, paramBuilder =>
            {
                paramBuilder.ToTable(TableName + nameof(AgvNodeAction) + nameof(OrderActionParameter));
                paramBuilder.Property(p => p.Key).HasMaxLength(DbStringLimits.Name);
                paramBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
                paramBuilder.HasKey(ShadowId);

                paramBuilder.Property(p => p.Value)
                    .HasConversion(EfValueConverters.CreateJsonConverter<object>()!);
            });
        });
    }
}