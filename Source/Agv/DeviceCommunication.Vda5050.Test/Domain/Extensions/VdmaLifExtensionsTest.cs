using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Shouldly;
using VdmaBlockingType = Vdma.Lif.BlockingType;
using VdmaControlPoint = Vdma.Lif.ControlPoint;
using VdmaEdge = Vdma.Lif.Edge;
using VdmaEdgeActionParameter = Vdma.Lif.PurpleActionParameter;
using VdmaLayout = Vdma.Lif.Layout;
using VdmaLifLayoutCollection = Vdma.Lif.LifLayoutCollection;
using VdmaLoadRestriction = Vdma.Lif.LoadRestriction;
using VdmaMetaInformation = Vdma.Lif.MetaInformation;
using VdmaNode = Vdma.Lif.Node;
using VdmaNodeActionParameter = Vdma.Lif.FluffyActionParameter;
using VdmaNodePosition = Vdma.Lif.NodePosition;
using VdmaRequirementType = Vdma.Lif.RequirementType;
using VdmaStation = Vdma.Lif.Station;
using VdmaStationPosition = Vdma.Lif.StationPosition;
using VdmaTrajectory = Vdma.Lif.Trajectory;
using VdmaVehicleTypeEdgeProperty = Vdma.Lif.VehicleTypeEdgeProperty;
using VdmaVehicleTypeEdgePropertyAction = Vdma.Lif.VehicleTypeEdgePropertyAction;
using VdmaVehicleTypeNodeProperty = Vdma.Lif.VehicleTypeNodeProperty;
using VdmaVehicleTypeNodePropertyAction = Vdma.Lif.VehicleTypeNodePropertyAction;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class VdmaLifExtensionsTest
{
   [TestMethod]
    public void ToMetaInformation_MapsAllProperties()
    {
        var timestamp = new DateTimeOffset(2024, 3, 15, 12, 0, 0, TimeSpan.Zero);
        var vdmaMeta = new VdmaMetaInformation
        {
            Creator = "TestCreator",
            ExportTimestamp = timestamp,
            LifVersion = "2.0.0",
            ProjectIdentification = "MyProject"
        };

        var result = vdmaMeta.ToMetaInformation();

        result.Creator.ShouldBe("TestCreator");
        result.ExportTimestamp.ShouldBe(timestamp);
        result.LifVersion.ShouldBe("2.0.0");
        result.ProjectIdentification.ShouldBe("MyProject");
    }

    [TestMethod]
    public void ToNodePosition_MapsXAndY()
    {
        var position = new VdmaNodePosition { X = 3.5, Y = -1.2 };

        var result = position.ToNodePosition();

        result.X.ShouldBe(3.5);
        result.Y.ShouldBe(-1.2);
    }

    [TestMethod]
    public void ToControlPoint_MapsXYAndWeight()
    {
        var cp = new VdmaControlPoint { X = 1.0, Y = 2.0, Weight = 0.75 };

        var result = cp.ToControlPoint();

        result.X.ShouldBe(1.0);
        result.Y.ShouldBe(2.0);
        result.Weight.ShouldBe(0.75);
    }

    [TestMethod]
    public void ToControlPoint_NullWeight_IsPreservedAsNull()
    {
        var cp = new VdmaControlPoint { X = 0.0, Y = 0.0, Weight = null };

        var result = cp.ToControlPoint();

        result.Weight.ShouldBeNull();
    }

    [TestMethod]
    public void ToTrajectory_MapsControlPointsKnotVectorAndDegree()
    {
        var trajectory = new VdmaTrajectory
        {
            ControlPoints = [new VdmaControlPoint { X = 1.0, Y = 2.0 }],
            KnotVector = [0.0, 1.0],
            Degree = 3
        };

        var result = trajectory.ToTrajectory();

        result.Degree.ShouldBe(3L);
        result.KnotVector.ShouldBe([0.0, 1.0]);
        result.ControlPoints.ShouldHaveSingleItem();
        result.ControlPoints.Single().X.ShouldBe(1.0);
        result.ControlPoints.Single().Y.ShouldBe(2.0);
    }

    [TestMethod]
    public void ToNodeActionParameter_MapsKeyAndValue()
    {
        var param = new VdmaNodeActionParameter { Key = "stationName", Value = "PS01" };

        var result = param.ToNodeActionParameter();

        result.Key.ShouldBe("stationName");
        result.Value.ShouldBe("PS01");
    }

    [TestMethod]
    public void ToEdgeActionParameter_MapsKeyAndValue()
    {
        var param = new VdmaEdgeActionParameter { Key = "maxSpeed", Value = "2.5" };

        var result = param.ToEdgeActionParameter();

        result.Key.ShouldBe("maxSpeed");
        result.Value.ShouldBe("2.5");
    }

    [TestMethod]
    public void ToStationPosition_MapsXYAndTheta()
    {
        var pos = new VdmaStationPosition { X = 5.0, Y = -3.0, Theta = 1.57 };

        var result = pos.ToStationPosition();

        result.X.ShouldBe(5.0);
        result.Y.ShouldBe(-3.0);
        result.Theta.ShouldBe(1.57);
    }

    [TestMethod]
    public void ToStationPosition_NullTheta_IsPreservedAsNull()
    {
        var pos = new VdmaStationPosition { X = 0.0, Y = 0.0, Theta = null };

        var result = pos.ToStationPosition();

        result.Theta.ShouldBeNull();
    }

    [TestMethod]
    public void ToLoadRestriction_MapsLoadedUnloadedAndLoadSetNames()
    {
        var restriction = new VdmaLoadRestriction
        {
            Loaded = true,
            Unloaded = false,
            LoadSetNames = ["SET_A", "SET_B"]
        };

        var result = restriction.ToLoadRestriction();

        result.Loaded.ShouldBeTrue();
        result.Unloaded.ShouldBeFalse();
        result.LoadSetNames.ShouldBe(["SET_A", "SET_B"]);
    }

    [TestMethod]
    public void ToVehicleTypeNodePropertyAction_HardBlockingType_MapsToHard()
    {
        var action = MakeNodeAction(VdmaBlockingType.Hard);

        action.ToVehicleTypeNodePropertyAction().BlockingType.ShouldBe(BlockingType.HARD);
    }

    [TestMethod]
    public void ToVehicleTypeNodePropertyAction_SoftBlockingType_MapsToSoft()
    {
        var action = MakeNodeAction(VdmaBlockingType.Soft);

        action.ToVehicleTypeNodePropertyAction().BlockingType.ShouldBe(BlockingType.SOFT);
    }

    [TestMethod]
    public void ToVehicleTypeNodePropertyAction_NoneBlockingType_MapsToNone()
    {
        var action = MakeNodeAction();

        action.ToVehicleTypeNodePropertyAction().BlockingType.ShouldBe(BlockingType.NONE);
    }

    [TestMethod]
    public void ToVehicleTypeNodePropertyAction_RequiredRequirementType_MapsToRequired()
    {
        var action = MakeNodeAction(requirementType: VdmaRequirementType.Required);

        action.ToVehicleTypeNodePropertyAction().RequirementType.ShouldBe(RequirementType.Required);
    }

    [TestMethod]
    public void ToVehicleTypeNodePropertyAction_OptionalRequirementType_MapsToOptional()
    {
        var action = MakeNodeAction(requirementType: VdmaRequirementType.Optional);

        action.ToVehicleTypeNodePropertyAction().RequirementType.ShouldBe(RequirementType.Optional);
    }

    [TestMethod]
    public void ToVehicleTypeNodePropertyAction_ConditionalRequirementType_MapsToConditional()
    {
        var action = MakeNodeAction(requirementType: VdmaRequirementType.Conditional);

        action.ToVehicleTypeNodePropertyAction().RequirementType.ShouldBe(RequirementType.Conditional);
    }

    [TestMethod]
    public void ToVehicleTypeNodePropertyAction_NullRequirementType_IsPreservedAsNull()
    {
        var action = new VdmaVehicleTypeNodePropertyAction
        {
            ActionType = "pick",
            BlockingType = VdmaBlockingType.None,
            RequirementType = null
        };

        action.ToVehicleTypeNodePropertyAction().RequirementType.ShouldBeNull();
    }

    [TestMethod]
    public void ToVehicleTypeNodeProperty_MapsVehicleTypeIdAndTheta()
    {
        var property = new VdmaVehicleTypeNodeProperty
        {
            VehicleTypeId = "AGV_TYPE_1",
            Theta = 1.0,
            Actions = null
        };

        var result = property.ToVehicleTypeNodeProperty();

        result.VehicleTypeId.ShouldBe("AGV_TYPE_1");
        result.Theta.ShouldBe(1.0);
    }

    [TestMethod]
    public void ToVehicleTypeNodeProperty_WithActions_MapsActions()
    {
        var property = new VdmaVehicleTypeNodeProperty
        {
            VehicleTypeId = "TYPE",
            Actions = [MakeNodeAction()]
        };

        var result = property.ToVehicleTypeNodeProperty();

        result.Actions.ShouldNotBeNull();
        result.Actions.ShouldHaveSingleItem();
    }

    [TestMethod]
    public void ToVehicleTypeNodeProperty_AssignsNewFleetManagerNodeId()
    {
        var p1 = new VdmaVehicleTypeNodeProperty { VehicleTypeId = "TYPE" };
        var p2 = new VdmaVehicleTypeNodeProperty { VehicleTypeId = "TYPE" };

        p1.ToVehicleTypeNodeProperty().FleetManagerNodeId
            .ShouldNotBe(p2.ToVehicleTypeNodeProperty().FleetManagerNodeId);
    }

    [TestMethod]
    public void ToNode_MapsAllProperties()
    {
        var node = new VdmaNode
        {
            NodeId = "N1",
            NodeName = "Node One",
            MapId = "MAP_1",
            NodeDescription = "A test node",
            NodePosition = new VdmaNodePosition { X = 2.0, Y = 3.0 },
            VehicleTypeNodeProperties = []
        };

        var result = node.ToNode();

        result.NodeId.ShouldBe("N1");
        result.NodeName.ShouldBe("Node One");
        result.MapId.ShouldBe("MAP_1");
        result.NodeDescription.ShouldBe("A test node");
        result.NodePosition.X.ShouldBe(2.0);
        result.NodePosition.Y.ShouldBe(3.0);
    }

    [TestMethod]
    public void ToNode_WithVehicleProperties_ConvertsEach()
    {
        var node = new VdmaNode
        {
            NodeId = "N1",
            NodePosition = new VdmaNodePosition { X = 0, Y = 0 },
            VehicleTypeNodeProperties = [
                new VdmaVehicleTypeNodeProperty { VehicleTypeId = "TYPE_A" },
                new VdmaVehicleTypeNodeProperty { VehicleTypeId = "TYPE_B" }
            ]
        };

        var result = node.ToNode();

        result.VehicleTypeNodeProperties.Count.ShouldBe(2);
    }

    [TestMethod]
    public void ToEdge_MapsAllProperties()
    {
        var edge = new VdmaEdge
        {
            EdgeId = "E1",
            StartNodeId = "N1",
            EndNodeId = "N2",
            VehicleTypeEdgeProperties = []
        };

        var result = edge.ToEdge();

        result.EdgeId.ShouldBe("E1");
        result.StartNodeId.ShouldBe("N1");
        result.EndNodeId.ShouldBe("N2");
    }

    [TestMethod]
    public void ToEdge_WithVehicleProperties_ConvertsEach()
    {
        var edge = new VdmaEdge
        {
            EdgeId = "E1",
            StartNodeId = "N1",
            EndNodeId = "N2",
            VehicleTypeEdgeProperties = [
                new VdmaVehicleTypeEdgeProperty { VehicleTypeId = "TYPE_A" },
                new VdmaVehicleTypeEdgeProperty { VehicleTypeId = "TYPE_B" }
            ]
        };

        var result = edge.ToEdge();

        result.VehicleTypeEdgeProperties.Count.ShouldBe(2);
    }

    [TestMethod]
    public void ToVehicleTypeEdgePropertyAction_MapsActionTypeAndBlockingType()
    {
        var action = new VdmaVehicleTypeEdgePropertyAction
        {
            ActionType = "customAction",
            BlockingType = VdmaBlockingType.Hard,
            ActionDescription = "desc",
            RequirementType = VdmaRequirementType.Optional
        };

        var result = action.ToVehicleTypeEdgePropertyAction();

        result.ActionType.ShouldBe("customAction");
        result.BlockingType.ShouldBe(BlockingType.HARD);
        result.ActionDescription.ShouldBe("desc");
        result.RequirementType.ShouldBe(RequirementType.Optional);
    }

    [TestMethod]
    public void ToVehicleTypeEdgeProperty_MapsVehicleTypeIdAndOptionals()
    {
        var property = new VdmaVehicleTypeEdgeProperty
        {
            VehicleTypeId = "AGV_TYPE",
            MaxSpeed = 1.5,
            MaxHeight = 2.0,
            MinHeight = 0.5,
            ReentryAllowed = true
        };

        var result = property.ToVehicleTypeEdgeProperty();

        result.VehicleTypeId.ShouldBe("AGV_TYPE");
        result.MaxSpeed.ShouldBe(1.5);
        result.MaxHeight.ShouldBe(2.0);
        result.MinHeight.ShouldBe(0.5);
        result.ReentryAllowed!.Value.ShouldBeTrue();
    }

    [TestMethod]
    public void ToVehicleTypeEdgeProperty_WithTrajectory_MapsTrajectory()
    {
        var property = new VdmaVehicleTypeEdgeProperty
        {
            VehicleTypeId = "TYPE",
            Trajectory = new VdmaTrajectory
            {
                ControlPoints = [new VdmaControlPoint { X = 1, Y = 2 }],
                KnotVector = [0.0, 1.0],
                Degree = 2
            }
        };

        var result = property.ToVehicleTypeEdgeProperty();

        result.Trajectory.ShouldNotBeNull();
        result.Trajectory.ControlPoints.ShouldHaveSingleItem();
    }

    [TestMethod]
    public void ToVehicleTypeEdgeProperty_WithLoadRestriction_MapsLoadRestriction()
    {
        var property = new VdmaVehicleTypeEdgeProperty
        {
            VehicleTypeId = "TYPE",
            LoadRestriction = new VdmaLoadRestriction { Loaded = true, Unloaded = false }
        };

        var result = property.ToVehicleTypeEdgeProperty();

        result.LoadRestriction.ShouldNotBeNull();
        result.LoadRestriction.Loaded.ShouldBeTrue();
        result.LoadRestriction.Unloaded.ShouldBeFalse();
    }

    [TestMethod]
    public void GetId_SameInputs_ReturnsSameGuid()
    {
        var meta = new MetaInformation("c", DateTimeOffset.UtcNow, "1.0", "proj-1");
        var layout = new VdmaLayout { LayoutId = "L1", LayoutLevelId = "floor1" };

        var id1 = layout.GetId(meta);
        var id2 = layout.GetId(meta);

        id1.ShouldBe(id2);
    }

    [TestMethod]
    public void GetId_DifferentProjectIdentification_ReturnsDifferentGuids()
    {
        var meta1 = new MetaInformation("c", DateTimeOffset.UtcNow, "1.0", "proj-1");
        var meta2 = new MetaInformation("c", DateTimeOffset.UtcNow, "1.0", "proj-2");
        var layout = new VdmaLayout { LayoutId = "L1", LayoutLevelId = "floor1" };

        layout.GetId(meta1).ShouldNotBe(layout.GetId(meta2));
    }

    [TestMethod]
    public void ToStation_MapsAllProperties()
    {
        var station = new VdmaStation
        {
            StationId = "S1",
            StationName = "Station One",
            StationDescription = "A station",
            StationHeight = 1.2,
            InteractionNodeIds = ["N1", "N2"],
            StationPosition = new VdmaStationPosition { X = 4.0, Y = 5.0, Theta = 0.5 }
        };

        var result = station.ToStation();

        result.StationId.ShouldBe("S1");
        result.StationName.ShouldBe("Station One");
        result.StationDescription.ShouldBe("A station");
        result.StationHeight.ShouldBe(1.2);
        result.InteractionNodeIds.ShouldBe(["N1", "N2"]);
        result.StationPosition.ShouldNotBeNull();
        result.StationPosition.X.ShouldBe(4.0);
    }

    [TestMethod]
    public void ToStation_NullStationPosition_IsPreservedAsNull()
    {
        var station = new VdmaStation
        {
            StationId = "S1",
            InteractionNodeIds = [],
            StationPosition = null
        };

        var result = station.ToStation();

        result.StationPosition.ShouldBeNull();
    }

    [TestMethod]
    public void ToLifLayoutCollection_MapsMetaInformation()
    {
        var timestamp = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var collection = new VdmaLifLayoutCollection
        {
            MetaInformation = new VdmaMetaInformation
            {
                Creator = "TestCreator",
                ExportTimestamp = timestamp,
                LifVersion = "1.0",
                ProjectIdentification = "ProjectX"
            },
            Layouts = []
        };

        var result = collection.ToLifLayoutCollection();

        result.MetaInformation.Creator.ShouldBe("TestCreator");
        result.MetaInformation.ProjectIdentification.ShouldBe("ProjectX");
    }

    [TestMethod]
    public void ToLifLayoutCollection_WithLayouts_ConvertsEach()
    {
        var collection = new VdmaLifLayoutCollection
        {
            MetaInformation = MakeVdmaMetaInfo(),
            Layouts =
            [
                new VdmaLayout { LayoutId = "L1", LayoutVersion = "1", Nodes = [], Edges = [], Stations = [] },
                new VdmaLayout { LayoutId = "L2", LayoutVersion = "1", Nodes = [], Edges = [], Stations = [] }
            ]
        };

        var result = collection.ToLifLayoutCollection();

        result.Layouts.Length.ShouldBe(2);
    }

    private static VdmaVehicleTypeNodePropertyAction MakeNodeAction(
        VdmaBlockingType blockingType = VdmaBlockingType.None,
        VdmaRequirementType? requirementType = VdmaRequirementType.Optional) =>
        new()
        {
            ActionType = "pick",
            BlockingType = blockingType,
            RequirementType = requirementType
        };

    private static VdmaMetaInformation MakeVdmaMetaInfo() =>
        new()
        {
            Creator = "creator",
            ExportTimestamp = DateTimeOffset.UtcNow,
            LifVersion = "1.0",
            ProjectIdentification = "proj"
        };
}
