using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// Entity configuration for <see cref="Edge"/>.
/// </summary>
public class EdgeEntityConfiguration : IEntityTypeConfiguration<Edge>
{
    private const string TableName = "DeviceCommunicationVda5050Edge";
    private const string ShadowId = "Id";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Edge> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Timestamp);
        builder.Property(e => e.EdgeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.StartNodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.EndNodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.OwnsOne(e => e.AgvTrajectory, trajectoryBuilder =>
        {
            trajectoryBuilder.Property(t => t.KnotVector)
                .HasConversion(EfValueConverters.CreateJsonConverter<IList<float>>())
                .Metadata.SetValueComparer(new ValueComparer<IList<float>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ));
            trajectoryBuilder.OwnsMany(t => t.ControlPoints, cpBuilder =>
            {
                cpBuilder.ToTable(TableName + nameof(ControlPoint));
                cpBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
                cpBuilder.HasKey(ShadowId);
            });
        });
        builder.OwnsMany(e => e.AgvActions, actionBuilder =>
        {
            actionBuilder.ToTable(TableName + nameof(AgvEdgeAction));
            actionBuilder.Property(a => a.ActionType).HasMaxLength(DbStringLimits.Name);
            actionBuilder.OwnsMany(a => a.Parameters, paramBuilder =>
            {
                paramBuilder.ToTable(TableName + nameof(AgvEdgeAction) + nameof(OrderActionParameter));
                paramBuilder.Property(p => p.Key).HasMaxLength(DbStringLimits.Name);
                paramBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
                paramBuilder.HasKey(ShadowId);

                paramBuilder.Property(p => p.Value)
                    .HasConversion(EfValueConverters.CreateJsonConverter<object>());
            });
        });
    }
}