using FluentValidation.TestHelper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Validators;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;
using Vdma.Lif;
using LifValidationResult = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif.LifValidationResult;
using Severity = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif.Severity;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Validators;

[TestClass]
public class LifLayoutCollectionValidatorTest
{
    private readonly LifLayoutCollectionValidator _validator = new();

    [TestMethod]
    public async Task Validate_EmptyLayouts_Fails()
    {
        var lif = new LifLayoutCollection { Layouts = [], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.ShouldHaveValidationErrorFor(x => x.Layouts);
    }

    [TestMethod]
    public async Task Validate_LayoutWithNoNodes_Fails()
    {
        var layout = LifGenerator.GenerateLayout(edges: [LifGenerator.GenerateEdge("e1", "n1", "n2")]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Validate_LayoutWithNoEdges_Fails()
    {
        var layout = LifGenerator.GenerateLayout(nodes: [LifGenerator.GenerateNode("n1")]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Validate_EdgeReferencesMissingStartNode_Fails()
    {
        var node = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "missing", "n2");
        var layout = LifGenerator.GenerateLayout(nodes: [node], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("StartNodeId") && e.ErrorMessage.Contains("missing"));
    }

    [TestMethod]
    public async Task Validate_EdgeReferencesMissingEndNode_Fails()
    {
        var node = LifGenerator.GenerateNode("n1");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "missing");
        var layout = LifGenerator.GenerateLayout(nodes: [node], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("EndNodeId") && e.ErrorMessage.Contains("missing"));
    }
    
    [TestMethod]
    public async Task Validate_OrientationUnreachableDueToNoRotationPermission_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var n3 = LifGenerator.GenerateNode("n3");
        var e1 = LifGenerator.GenerateEdge("e1", "n1", "n2",
            [LifGenerator.GenerateVehicleTypeEdgeProperty(vehicleOrientation: Math.PI, rotationAtEndNodeAllowed: "NONE")]);
        var e2 = LifGenerator.GenerateEdge("e2", "n2", "n3",
            [LifGenerator.GenerateVehicleTypeEdgeProperty(vehicleOrientation: 0, rotationAllowed: false, rotationAtStartNodeAllowed: "NONE")]);
        var station = LifGenerator.GenerateStation("s1", ["n1"]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2, n3], edges: [e1, e2], stations: [station]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e2") && ((LifValidationResult)e.CustomState!).Severity == Severity.Warn);
    }

    [TestMethod]
    public async Task Validate_UnsupportedLifVersion_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation("2.0.0") };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("version") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_DuplicateLayoutId_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout1 = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var layout2 = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout1, layout2], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("LayoutId") && e.ErrorMessage.Contains("duplicated") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_DuplicateStationId_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var station1 = LifGenerator.GenerateStation("s1", ["n1"]);
        var station2 = LifGenerator.GenerateStation("s1", ["n2"]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge], stations: [station1, station2]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("StationId") && e.ErrorMessage.Contains("duplicated") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_DuplicateEdgeId_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var n3 = LifGenerator.GenerateNode("n3");
        var edge1 = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var edge2 = LifGenerator.GenerateEdge("e1", "n2", "n3", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2, n3], edges: [edge1, edge2]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("EdgeId") && e.ErrorMessage.Contains("duplicated") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_DuplicateNodeId_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n1Duplicate = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n1Duplicate, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("NodeId") && e.ErrorMessage.Contains("duplicated") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_DuplicateNodeIdAcrossLayouts_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge1 = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout1 = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge1]);

        var n1OtherLayout = LifGenerator.GenerateNode("n1");
        var n3 = LifGenerator.GenerateNode("n3");
        var edge2 = LifGenerator.GenerateEdge("e2", "n1", "n3", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout2 = LifGenerator.GenerateLayout("layout-2", nodes: [n1OtherLayout, n3], edges: [edge2]);

        var lif = new LifLayoutCollection { Layouts = [layout1, layout2], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("NodeId") && e.ErrorMessage.Contains("'n1'") && e.ErrorMessage.Contains("duplicated") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_DuplicateEdgeIdAcrossLayouts_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge1 = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout1 = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge1]);

        var n3 = LifGenerator.GenerateNode("n3");
        var n4 = LifGenerator.GenerateNode("n4");
        var edge1OtherLayout = LifGenerator.GenerateEdge("e1", "n3", "n4", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout2 = LifGenerator.GenerateLayout("layout-2", nodes: [n3, n4], edges: [edge1OtherLayout]);

        var lif = new LifLayoutCollection { Layouts = [layout1, layout2], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("EdgeId") && e.ErrorMessage.Contains("'e1'") && e.ErrorMessage.Contains("duplicated") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_DuplicateStationIdAcrossLayouts_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge1 = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var station1 = LifGenerator.GenerateStation("s1", ["n1"]);
        var layout1 = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge1], stations: [station1]);

        var n3 = LifGenerator.GenerateNode("n3");
        var n4 = LifGenerator.GenerateNode("n4");
        var edge2 = LifGenerator.GenerateEdge("e2", "n3", "n4", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var station1OtherLayout = LifGenerator.GenerateStation("s1", ["n3"]);
        var layout2 = LifGenerator.GenerateLayout("layout-2", nodes: [n3, n4], edges: [edge2], stations: [station1OtherLayout]);

        var lif = new LifLayoutCollection { Layouts = [layout1, layout2], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("StationId") && e.ErrorMessage.Contains("'s1'") && e.ErrorMessage.Contains("duplicated") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_EdgeWithoutVehicleTypeEdgeProperties_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2");
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("vehicle type edge properties") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [DataRow("MaxHeight")]
    [DataRow("MinHeight")]
    [DataRow("MaxSpeed")]
    [DataRow("MaxRotationSpeed")]
    [TestMethod]
    public async Task Validate_NegativeEdgeValueRange_ReportsErr(string property)
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edgeProperty = property switch
        {
            "MaxHeight" => LifGenerator.GenerateVehicleTypeEdgeProperty(maxHeight: -1),
            "MinHeight" => LifGenerator.GenerateVehicleTypeEdgeProperty(minHeight: -1),
            "MaxSpeed" => LifGenerator.GenerateVehicleTypeEdgeProperty(maxSpeed: -1),
            "MaxRotationSpeed" => LifGenerator.GenerateVehicleTypeEdgeProperty(maxRotationSpeed: -1),
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains(property) && e.ErrorMessage.Contains("must be >=0") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_NodeNotConnectedToAnyEdge_ReportsWarn()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var isolated = LifGenerator.GenerateNode("isolated");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2, isolated], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("isolated") && e.ErrorMessage.Contains("not connected to any edge") && ((LifValidationResult)e.CustomState!).Severity == Severity.Warn);
    }

    [TestMethod]
    public async Task Validate_DeadNode_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var n3 = LifGenerator.GenerateNode("n3");
        var e1 = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var e2 = LifGenerator.GenerateEdge("e2", "n2", "n3", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2, n3], edges: [e1, e2]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Dead node") && e.ErrorMessage.Contains("n3") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_UnreachableNode_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var n3 = LifGenerator.GenerateNode("n3");
        var e1 = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var e2 = LifGenerator.GenerateEdge("e2", "n2", "n3", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2, n3], edges: [e1, e2]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Unreachable node") && e.ErrorMessage.Contains("n1") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_NodeWithoutVehicleTypeNodeProperties_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2", vehicleTypeProps: [LifGenerator.GenerateVehicleTypeNodeProperty()]);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var returnEdge = LifGenerator.GenerateEdge("e2", "n2", "n1", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge, returnEdge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("n1") && e.ErrorMessage.Contains("vehicle type node properties") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_NodeThetaOutOfRange_ReportsWarn()
    {
        var n1 = LifGenerator.GenerateNode("n1", vehicleTypeProps: [LifGenerator.GenerateVehicleTypeNodeProperty(theta: 4.0)]);
        var n2 = LifGenerator.GenerateNode("n2", vehicleTypeProps: [LifGenerator.GenerateVehicleTypeNodeProperty()]);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var returnEdge = LifGenerator.GenerateEdge("e2", "n2", "n1", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge, returnEdge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("n1") && e.ErrorMessage.Contains("Theta") && ((LifValidationResult)e.CustomState!).Severity == Severity.Warn);
    }

    [TestMethod]
    public async Task Validate_StationWithoutInteractionNodeIds_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var station = LifGenerator.GenerateStation("s1", []);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge], stations: [station]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("s1") && e.ErrorMessage.Contains("does not define interaction nodes") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_StationReferencesMissingInteractionNode_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var station = LifGenerator.GenerateStation("s1", ["missing"]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge], stations: [station]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("s1") && e.ErrorMessage.Contains("missing") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_StationThetaOutOfRange_ReportsWarn()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty()]);
        var station = LifGenerator.GenerateStation("s1", ["n1"], new StationPosition { X = 0, Y = 0, Theta = 4.0 });
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge], stations: [station]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("s1") && e.ErrorMessage.Contains("Theta") && ((LifValidationResult)e.CustomState!).Severity == Severity.Warn);
    }

    [TestMethod]
    public async Task Validate_TrajectoryWithFewerThanTwoControlPoints_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1", x: 0, y: 0);
        var n2 = LifGenerator.GenerateNode("n2", x: 5, y: 0);
        var trajectory = new Trajectory
        {
            ControlPoints = [new ControlPoint { X = 0, Y = 0 }],
            KnotVector = [0, 1],
            Degree = 1
        };
        var edgeProperty = LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: trajectory);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("less than 2 control points") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_TrajectoryControlPointCountInconsistentWithDegree_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1", x: 0, y: 0);
        var n2 = LifGenerator.GenerateNode("n2", x: 5, y: 0);
        var trajectory = new Trajectory
        {
            ControlPoints = [new ControlPoint { X = 0, Y = 0 }, new ControlPoint { X = 5, Y = 0 }],
            KnotVector = [0, 0, 0, 1, 1],
            Degree = 2
        };
        var edgeProperty = LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: trajectory);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("inconsistent with degree") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_TrajectoryKnotVectorLengthInconsistent_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1", x: 0, y: 0);
        var n2 = LifGenerator.GenerateNode("n2", x: 5, y: 0);
        var trajectory = new Trajectory
        {
            ControlPoints = [new ControlPoint { X = 0, Y = 0 }, new ControlPoint { X = 5, Y = 0 }],
            KnotVector = [0, 0, 1],
            Degree = 1
        };
        var edgeProperty = LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: trajectory);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("inconsistent with number of control points") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_TrajectoryKnotVectorValuesOutOfRange_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1", x: 0, y: 0);
        var n2 = LifGenerator.GenerateNode("n2", x: 5, y: 0);
        var trajectory = new Trajectory
        {
            ControlPoints = [new ControlPoint { X = 0, Y = 0 }, new ControlPoint { X = 5, Y = 0 }],
            KnotVector = [0, 0, 1, 1.5],
            Degree = 1
        };
        var edgeProperty = LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: trajectory);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("range [0,1]") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_TrajectoryWithEmptyKnotVector_ReportsErr()
    {
        var node1 = LifGenerator.GenerateNode("n1", x: 0, y: 0);
        var node2 = LifGenerator.GenerateNode("n2", x: 5, y: 0);
        var trajectory = new Trajectory
        {
            ControlPoints = [new ControlPoint { X = 0, Y = 0 }, new ControlPoint { X = 5, Y = 0 }],
            KnotVector = [],
            Degree = 1
        };
        var edgeProperty = LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: trajectory);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [node1, node2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("inconsistent with number of control points") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
        result.Errors.ShouldNotContain(e => e.ErrorMessage.Contains("range [0,1]"));
    }

    [TestMethod]
    public async Task Validate_ClampedQuinticTrajectory_Passes()
    {
        var node1 = LifGenerator.GenerateNode("n1", x: 0, y: 0, vehicleTypeProps: [LifGenerator.GenerateVehicleTypeNodeProperty()]);
        var node2 = LifGenerator.GenerateNode("n2", x: 5, y: 0, vehicleTypeProps: [LifGenerator.GenerateVehicleTypeNodeProperty()]);
        var edgeTrajectory = new Trajectory
        {
            ControlPoints =
            [
                new ControlPoint { X = 0, Y = 0 },
                new ControlPoint { X = 1, Y = 1 },
                new ControlPoint { X = 2, Y = 2 },
                new ControlPoint { X = 3, Y = 2 },
                new ControlPoint { X = 4, Y = 1 },
                new ControlPoint { X = 5, Y = 0 }
            ],
            KnotVector = [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1],
            Degree = 5
        };
        var returnEdgeTrajectory = new Trajectory
        {
            ControlPoints =
            [
                new ControlPoint { X = 5, Y = 0 },
                new ControlPoint { X = 4, Y = 1 },
                new ControlPoint { X = 2, Y = 2 },
                new ControlPoint { X = 1, Y = 1 },
                new ControlPoint { X = 3, Y = 2 },
                new ControlPoint { X = 0, Y = 0 }
            ],
            KnotVector = [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1],
            Degree = 5
        };

        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: edgeTrajectory)]);
        var returnEdge = LifGenerator.GenerateEdge("e2", "n2", "n1", [LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: returnEdgeTrajectory)]);
        var layout = LifGenerator.GenerateLayout(nodes: [node1, node2], edges: [edge, returnEdge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task Validate_TrajectoryDegreeLessThanOne_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1", x: 0, y: 0);
        var n2 = LifGenerator.GenerateNode("n2", x: 5, y: 0);
        var trajectory = new Trajectory
        {
            ControlPoints = [new ControlPoint { X = 0, Y = 0 }],
            KnotVector = [0, 1],
            Degree = 0
        };
        var edgeProperty = LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: trajectory);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("degree needs to be in range") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_TrajectoryStartControlPointMismatchesStartNode_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1", x: 0, y: 0);
        var n2 = LifGenerator.GenerateNode("n2", x: 5, y: 0);
        var trajectory = new Trajectory
        {
            ControlPoints = [new ControlPoint { X = 1, Y = 1 }, new ControlPoint { X = 5, Y = 0 }],
            KnotVector = [0, 0, 1, 1],
            Degree = 1
        };
        var edgeProperty = LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: trajectory);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("first control point") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }

    [TestMethod]
    public async Task Validate_TrajectoryEndControlPointMismatchesEndNode_ReportsErr()
    {
        var n1 = LifGenerator.GenerateNode("n1", x: 0, y: 0);
        var n2 = LifGenerator.GenerateNode("n2", x: 5, y: 0);
        var trajectory = new Trajectory
        {
            ControlPoints = [new ControlPoint { X = 0, Y = 0 }, new ControlPoint { X = 9, Y = 9 }],
            KnotVector = [0, 0, 1, 1],
            Degree = 1
        };
        var edgeProperty = LifGenerator.GenerateVehicleTypeEdgeProperty(trajectory: trajectory);
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2", [edgeProperty]);
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout], MetaInformation = LifGenerator.GenerateMetaInformation() };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("e1") && e.ErrorMessage.Contains("last control point") && ((LifValidationResult)e.CustomState!).Severity == Severity.Err);
    }
}