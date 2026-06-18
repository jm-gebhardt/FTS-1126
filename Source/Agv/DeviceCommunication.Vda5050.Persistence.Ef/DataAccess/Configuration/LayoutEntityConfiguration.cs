using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc/>
public class LayoutEntityConfiguration : IEntityTypeConfiguration<Layout>
{
    private const string TableName = "DeviceCommunicationVda5050Layout";
    private const string ShadowId = "Id";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Layout> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LayoutId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.LayoutVersion).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.LayoutDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.LayoutLevelId).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.LayoutName).HasMaxLength(DbStringLimits.Name);

        builder.OwnsMany(e => e.Nodes, ConfigureNode);
        builder.OwnsMany(e => e.Edges, ConfigureEdge);
        builder.OwnsMany(e => e.Stations, ConfigureStation);
    }

    private static void ConfigureNode(OwnedNavigationBuilder<Layout, Node> builder)
    {
        builder.ToTable(TableName + nameof(Node));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.NodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.MapId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.NodeDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.NodeName).HasMaxLength(DbStringLimits.Name);
        builder.OwnsOne(e => e.NodePosition);
        builder.OwnsMany(e => e.VehicleTypeNodeProperties, ConfigureVehicleTypeNodeProperty);
    }

    private static void ConfigureVehicleTypeNodeProperty(OwnedNavigationBuilder<Node, VehicleTypeNodeProperty> builder)
    {
        builder.ToTable(TableName + nameof(VehicleTypeNodeProperty));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.FleetManagerNodeId);
        builder.Property(e => e.VehicleTypeId).HasMaxLength(DbStringLimits.Name);
        builder.OwnsMany(e => e.Actions, ConfigureVehicleTypeNodePropertyAction);
    }

    private static void ConfigureVehicleTypeNodePropertyAction(OwnedNavigationBuilder<VehicleTypeNodeProperty, VehicleTypeNodePropertyAction> builder)
    {
        builder.ToTable(TableName + nameof(VehicleTypeNodePropertyAction));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.ActionType).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.ActionDescription).HasMaxLength(DbStringLimits.Description);
        builder.OwnsMany(e => e.ActionParameters, ConfigureNodeActionParameter);
    }

    private static void ConfigureNodeActionParameter(OwnedNavigationBuilder<VehicleTypeNodePropertyAction, NodeActionParameter> builder)
    {
        builder.ToTable(TableName + nameof(NodeActionParameter));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.Key).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.Value).HasMaxLength(DbStringLimits.Name);
    }

    private static void ConfigureEdge(OwnedNavigationBuilder<Layout, Edge> builder)
    {
        builder.ToTable(TableName + nameof(Edge));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.EdgeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.EndNodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.StartNodeId).HasMaxLength(DbStringLimits.NodeName);
        builder.OwnsMany(e => e.VehicleTypeEdgeProperties, ConfigureVehicleTypeEdgeProperty);
    }

    private static void ConfigureVehicleTypeEdgeProperty(OwnedNavigationBuilder<Edge, VehicleTypeEdgeProperty> builder)
    {
        builder.ToTable(TableName + nameof(VehicleTypeEdgeProperty));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.FleetManagerEdgeId);
        builder.Property(e => e.VehicleTypeId).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.OrientationType).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.RotationAtEndNodeAllowed).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.RotationAtStartNodeAllowed).HasMaxLength(DbStringLimits.Name);
        builder.OwnsOne(e => e.LoadRestriction, ConfigureLoadRestriction);
        builder.OwnsOne(e => e.Trajectory, ConfigureTrajectory);
        builder.OwnsMany(e => e.Actions, ConfigureVehicleTypeEdgePropertyAction);
    }

    private static void ConfigureLoadRestriction(OwnedNavigationBuilder<VehicleTypeEdgeProperty, LoadRestriction> builder)
    {
        builder.Property(e => e.LoadSetNames)
            .HasConversion(EfValueConverters.CreateNullableJsonConverter<string[]>())
            .Metadata.SetValueComparer(EfValueConverters.CreateArrayComparer<string>());
    }

    private static void ConfigureTrajectory(OwnedNavigationBuilder<VehicleTypeEdgeProperty, Trajectory> builder)
    {
        builder.Property(e => e.KnotVector)
            .HasConversion(EfValueConverters.CreateJsonConverter<double[]>())
            .Metadata.SetValueComparer(EfValueConverters.CreateArrayComparer<double>());
        builder.OwnsMany(e => e.ControlPoints, cpBuilder =>
        {
            cpBuilder.ToTable(TableName + nameof(ControlPoint));
            cpBuilder.Property<int>(ShadowId).ValueGeneratedOnAdd();
            cpBuilder.HasKey(ShadowId);
        });
    }

    private static void ConfigureVehicleTypeEdgePropertyAction(OwnedNavigationBuilder<VehicleTypeEdgeProperty, VehicleTypeEdgePropertyAction> builder)
    {
        builder.ToTable(TableName + nameof(VehicleTypeEdgePropertyAction));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.ActionType).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.ActionDescription).HasMaxLength(DbStringLimits.Description);
        builder.OwnsMany(e => e.ActionParameters, ConfigureEdgeActionParameter);
    }

    private static void ConfigureEdgeActionParameter(OwnedNavigationBuilder<VehicleTypeEdgePropertyAction, EdgeActionParameter> builder)
    {
        builder.ToTable(TableName + nameof(EdgeActionParameter));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.Key).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.Value).HasMaxLength(DbStringLimits.Name);
    }

    private static void ConfigureStation(OwnedNavigationBuilder<Layout, Station> builder)
    {
        builder.ToTable(TableName + nameof(Station));
        builder.Property<int>(ShadowId).ValueGeneratedOnAdd();
        builder.HasKey(ShadowId);
        builder.Property(e => e.StationId).HasMaxLength(DbStringLimits.NodeName);
        builder.Property(e => e.StationDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.StationName).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.InteractionNodeIds)
            .HasConversion(EfValueConverters.CreateJsonConverter<string[]>())
            .Metadata.SetValueComparer(EfValueConverters.CreateArrayComparer<string>());
        builder.Property(e => e.FleetManagerNodeIds)
            .HasConversion(EfValueConverters.CreateJsonConverter<Guid[]>())
            .Metadata.SetValueComparer(EfValueConverters.CreateArrayComparer<Guid>());
        builder.OwnsOne(e => e.StationPosition);
    }
}
