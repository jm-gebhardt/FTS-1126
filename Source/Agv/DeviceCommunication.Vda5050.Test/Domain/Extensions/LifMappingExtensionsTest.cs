using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Shouldly;
using Vdma.Lif;
using DomainNode = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.Node;
using DomainTrajectory = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.Trajectory;
using DomainControlPoint = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.ControlPoint;
using DomainOrientationType = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.OrientationType;
using DomainRequirementType = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.RequirementType;
using DomainNodePosition = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.NodePosition;
using CommonBlockingType = Gebhardt.Acc.Common.Domain.Models.BlockingType;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class LifMappingExtensionsTest
{
    [TestMethod]
    public void ToCommonBlockingType_Hard_ReturnsHARD()
    {
        BlockingType.Hard.ToCommonBlockingType().ShouldBe(CommonBlockingType.HARD);
    }

    [TestMethod]
    public void ToCommonBlockingType_Soft_ReturnsSOFT()
    {
        BlockingType.Soft.ToCommonBlockingType().ShouldBe(CommonBlockingType.SOFT);
    }

    [TestMethod]
    public void ToCommonBlockingType_None_ReturnsNONE()
    {
        BlockingType.None.ToCommonBlockingType().ShouldBe(CommonBlockingType.NONE);
    }

    [TestMethod]
    public void ToDomainRequirementType_Required()
    {
        RequirementType? rt = RequirementType.Required;

        rt.ToDomainRequirementType().ShouldBe(DomainRequirementType.Required);
    }

    [TestMethod]
    public void ToDomainRequirementType_Optional()
    {
        RequirementType? rt = RequirementType.Optional;

        rt.ToDomainRequirementType().ShouldBe(DomainRequirementType.Optional);
    }

    [TestMethod]
    public void ToDomainRequirementType_Conditional()
    {
        RequirementType? rt = RequirementType.Conditional;

        rt.ToDomainRequirementType().ShouldBe(DomainRequirementType.Conditional);
    }

    [TestMethod]
    public void ToDomainRequirementType_Null_ReturnsNull()
    {
        RequirementType? rt = null;

        rt.ToDomainRequirementType().ShouldBeNull();
    }

    [TestMethod]
    public void ToDomainOrientationType_Tangential()
    {
        "TANGENTIAL".ToDomainOrientationType().ShouldBe(DomainOrientationType.Tangential);
    }

    [TestMethod]
    public void ToDomainOrientationType_CaseInsensitive()
    {
        "tangential".ToDomainOrientationType().ShouldBe(DomainOrientationType.Tangential);
    }

    [TestMethod]
    public void ToDomainOrientationType_Null_ReturnsGlobal()
    {
        ((string?)null).ToDomainOrientationType().ShouldBe(DomainOrientationType.Global);
    }

    [TestMethod]
    public void ToDomainOrientationType_Unknown_ReturnsGlobal()
    {
        "something".ToDomainOrientationType().ShouldBe(DomainOrientationType.Global);
    }

    [TestMethod]
    public void ToNurbs_Null_ReturnsNull()
    {
        DomainTrajectory? trajectory = null;

        trajectory.ToNurbs().ShouldBeNull();
    }

    [TestMethod]
    public void ToNurbs_EmptyControlPoints_ReturnsNull()
    {
        var trajectory = new DomainTrajectory { Degree = 1, KnotVector = [0f, 1f], ControlPoints = [] };

        trajectory.ToNurbs().ShouldBeNull();
    }

    [TestMethod]
    public void ToNurbs_ValidTrajectory_MapsCorrectly()
    {
        var trajectory = new DomainTrajectory
        {
            Degree = 3,
            KnotVector = [0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f],
            ControlPoints =
            [
                new DomainControlPoint { X = 1.0, Y = 2.0, Weight = 1.0 },
                new DomainControlPoint { X = 3.0, Y = 4.0, Weight = 0.5 }
            ]
        };

        var result = trajectory.ToNurbs();

        result.ShouldNotBeNull();
        result.Degree.ShouldBe(3);
        result.KnotVector.Length.ShouldBe(8);
        result.ControlPoints.Length.ShouldBe(2);
        result.ControlPoints[0].X.ShouldBe(1.0);
        result.ControlPoints[1].Weight.ShouldBe(0.5);
    }

    [TestMethod]
    public void ToDomainTrajectory_NullLifTrajectory_ReturnsEmptyTrajectory()
    {
        Trajectory? lifTrajectory = null;

        var result = lifTrajectory.ToDomainTrajectory();

        result.ControlPoints.ShouldBeEmpty();
    }

    [TestMethod]
    public void ToDomainTrajectory_ValidLifTrajectory_Maps()
    {
        var lifTrajectory = new Trajectory
        {
            Degree = 2,
            KnotVector = [0.0, 0.5, 1.0],
            ControlPoints =
            [
                new ControlPoint { X = 1.0, Y = 2.0, Weight = 1.0 }
            ]
        };

        var result = lifTrajectory.ToDomainTrajectory();

        result.Degree.ShouldBe(2);
        result.KnotVector.Count.ShouldBe(3);
        result.ControlPoints.Count.ShouldBe(1);
        result.ControlPoints.First().X.ShouldBe(1.0);
    }

    [TestMethod]
    public void ToDomainParameters_FluffyNull_ReturnsEmpty()
    {
        FluffyActionParameter[]? parameters = null;

        parameters.ToDomainParameters().ShouldBeEmpty();
    }

    [TestMethod]
    public void ToDomainParameters_FluffyEmpty_ReturnsEmpty()
    {
        FluffyActionParameter[] parameters = [];

        parameters.ToDomainParameters().ShouldBeEmpty();
    }

    [TestMethod]
    public void ToDomainParameters_FluffyWithValues_Maps()
    {
        FluffyActionParameter[] parameters =
        [
            new() { Key = "key1", Value = "val1" }
        ];

        ICollection<OrderActionParameter> result = parameters.ToDomainParameters();

        result.Count.ShouldBe(1);
        result.Single().Key.ShouldBe("key1");
        result.Single().Value.ShouldBe("val1");
    }

    [TestMethod]
    public void ToDomainParameters_PurpleNull_ReturnsEmpty()
    {
        PurpleActionParameter[]? parameters = null;

        parameters.ToDomainParameters().ShouldBeEmpty();
    }

    [TestMethod]
    public void ToDomainParameters_PurpleWithValues_Maps()
    {
        PurpleActionParameter[] parameters =
        [
            new() { Key = "k", Value = "v" }
        ];

        ICollection<OrderActionParameter> result = parameters.ToDomainParameters();

        result.Count.ShouldBe(1);
        result.Single().Key.ShouldBe("k");
    }

    [TestMethod]
    public void GetVehicleTypeIds_FromLifLayoutCollection()
    {
        var lif = new LifLayoutCollection
        {
            Layouts =
            [
                new Layout
                {
                    LayoutId = "L1",
                    LayoutVersion = "1",
                    Nodes =
                    [
                        new Node
                        {
                            NodeId = "n1",
                            NodePosition = new NodePosition { X = 0, Y = 0 },
                            VehicleTypeNodeProperties =
                            [
                                new VehicleTypeNodeProperty { VehicleTypeId = "Type.A" },
                                new VehicleTypeNodeProperty { VehicleTypeId = "Type.B" }
                            ]
                        }
                    ],
                    Edges =
                    [
                        new Edge
                        {
                            EdgeId = "e1",
                            StartNodeId = "n1",
                            EndNodeId = "n1",
                            VehicleTypeEdgeProperties =
                            [
                                new VehicleTypeEdgeProperty { VehicleTypeId = "Type.A" }
                            ]
                        }
                    ],
                    Stations = []
                }
            ]
        };

        HashSet<string> result = lif.GetVehicleTypeIds();

        result.Count.ShouldBe(2);
        result.ShouldContain("Type.A");
        result.ShouldContain("Type.B");
    }

    [TestMethod]
    public void ToCommonGraphNode_MapsPositionAndName()
    {
        var domainNode = new DomainNode
        {
            Id = Guid.NewGuid(),
            NodeId = "node-1",
            Position = new DomainNodePosition(5.5, 10.2),
            AgvActions = []
        };
        var timestamp = new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);

        var result = domainNode.ToCommonGraphNode(timestamp);

        result.Id.ShouldBe(domainNode.Id);
        result.Name.ShouldBe("node-1");
        result.X.ShouldBe(5.5f);
        result.Y.ShouldBe(10.2f);
        result.TimeStamp.ShouldBe(timestamp);
    }
}








