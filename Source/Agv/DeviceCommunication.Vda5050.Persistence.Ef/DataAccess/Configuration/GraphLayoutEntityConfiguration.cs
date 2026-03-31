using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// Entity configuration for <see cref="GraphLayout"/> and its children.
/// </summary>
public class GraphLayoutEntityConfiguration : IEntityTypeConfiguration<GraphLayout>
{
    private const string TablePrefix = "DeviceCommunicationVda5050";
    private const string ShadowId = "Id";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<GraphLayout> builder)
    {
        builder.ToTable(TablePrefix + nameof(GraphLayout));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LayoutId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.LayoutVersion).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.LayoutName).HasMaxLength(DbStringLimits.Name);
        builder.HasIndex(e => new { e.LayoutId, e.LayoutVersion }).IsUnique();

        builder.HasMany(e => e.Nodes)
            .WithOne()
            .HasForeignKey(TablePrefix + "GraphLayoutId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Nodes).AutoInclude();

        builder.HasMany(e => e.Edges)
            .WithOne()
            .HasForeignKey(TablePrefix + "GraphLayoutId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Edges).AutoInclude();

        builder.OwnsMany(e => e.Stations, ConfigureStation);
    }

    private static void ConfigureStation(OwnedNavigationBuilder<GraphLayout, GraphLayoutStation> builder)
    {
        builder.ToTable(TablePrefix + nameof(GraphLayoutStation));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.StationId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.StationName).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.InteractionNodeIds)
            .HasConversion(EfValueConverters.CreateJsonConverter<ICollection<string>>()!)
            .Metadata.SetValueComparer(new ValueComparer<ICollection<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));
    }
}

/// <summary>
/// Entity configuration for <see cref="GraphLayoutNode"/>.
/// </summary>
public class GraphLayoutNodeEntityConfiguration : IEntityTypeConfiguration<GraphLayoutNode>
{
    private const string TablePrefix = "DeviceCommunicationVda5050";
    private const string ShadowId = "Id";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<GraphLayoutNode> builder)
    {
        builder.ToTable(TablePrefix + nameof(GraphLayoutNode));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.NodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.NodeName).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.MapId).HasMaxLength(DbStringLimits.NodeName);

        builder.OwnsMany(e => e.VehicleTypeProperties, vpBuilder =>
        {
            vpBuilder.ToTable(TablePrefix + nameof(NodeVehicleTypeProperty));
            vpBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
            vpBuilder.HasKey(ShadowId);
            vpBuilder.Property(e => e.VehicleTypeId).HasMaxLength(DbStringLimits.Name);

            vpBuilder.OwnsMany(vp => vp.Actions, actionBuilder =>
            {
                actionBuilder.ToTable(TablePrefix + nameof(NodeVehicleTypeAction));
                actionBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
                actionBuilder.HasKey(ShadowId);
                actionBuilder.Property(a => a.ActionType).HasMaxLength(DbStringLimits.Name);

                actionBuilder.OwnsMany(a => a.Parameters, paramBuilder =>
                {
                    paramBuilder.ToTable(TablePrefix + nameof(NodeVehicleTypeAction) + nameof(OrderActionParameter));
                    paramBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
                    paramBuilder.HasKey(ShadowId);
                    paramBuilder.Property(p => p.Key).HasMaxLength(DbStringLimits.Name);
                    paramBuilder.Property(p => p.Value)
                        .HasConversion(EfValueConverters.CreateJsonConverter<object>()!);
                });
            });
        });
    }
}

/// <summary>
/// Entity configuration for <see cref="GraphLayoutEdge"/>.
/// </summary>
public class GraphLayoutEdgeEntityConfiguration : IEntityTypeConfiguration<GraphLayoutEdge>
{
    private const string TablePrefix = "DeviceCommunicationVda5050";
    private const string ShadowId = "Id";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<GraphLayoutEdge> builder)
    {
        builder.ToTable(TablePrefix + nameof(GraphLayoutEdge));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EdgeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.StartNodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.EndNodeId).HasMaxLength(DbStringLimits.NodeName);

        builder.OwnsMany(e => e.VehicleTypeProperties, vpBuilder =>
        {
            vpBuilder.ToTable(TablePrefix + nameof(EdgeVehicleTypeProperty));
            vpBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
            vpBuilder.HasKey(ShadowId);
            vpBuilder.Property(e => e.VehicleTypeId).HasMaxLength(DbStringLimits.Name);

            vpBuilder.OwnsOne(vp => vp.Trajectory, trajectoryBuilder =>
            {
                trajectoryBuilder.Property(t => t.KnotVector)
                    .HasConversion(EfValueConverters.CreateJsonConverter<IList<float>>()!)
                    .Metadata.SetValueComparer(new ValueComparer<IList<float>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
                trajectoryBuilder.OwnsMany(t => t.ControlPoints, cpBuilder =>
                {
                    cpBuilder.ToTable(TablePrefix + nameof(EdgeVehicleTypeProperty) + nameof(ControlPoint));
                    cpBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
                    cpBuilder.HasKey(ShadowId);
                });
            });

            vpBuilder.OwnsMany(vp => vp.Actions, actionBuilder =>
            {
                actionBuilder.ToTable(TablePrefix + nameof(EdgeVehicleTypeAction));
                actionBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
                actionBuilder.HasKey(ShadowId);
                actionBuilder.Property(a => a.ActionType).HasMaxLength(DbStringLimits.Name);

                actionBuilder.OwnsMany(a => a.Parameters, paramBuilder =>
                {
                    paramBuilder.ToTable(TablePrefix + nameof(EdgeVehicleTypeAction) + nameof(OrderActionParameter));
                    paramBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
                    paramBuilder.HasKey(ShadowId);
                    paramBuilder.Property(p => p.Key).HasMaxLength(DbStringLimits.Name);
                    paramBuilder.Property(p => p.Value)
                        .HasConversion(EfValueConverters.CreateJsonConverter<object>()!);
                });
            });
        });
    }
}