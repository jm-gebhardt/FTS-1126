using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Route.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using OrderTrajectory = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.Trajectory;
using OrientationType = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders.OrientationType;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services.Routes;

[TestClass]
public class RouteStepTranslatorTest
{
    private const string StartNodeId = "start";
    private const string EndNodeId = "end";
    private const string EdgeId = "edge1";

    private readonly Vda5050.Domain.Models.Agvs.Agv _agv = AgvGenerator.Generate(Guid.NewGuid());
    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IAgvActionRepository> _agvActionRepository = new();
    private readonly Mock<ILifRepository> _lifRepository = new();
    private readonly RouteStepTranslator _translator;

    public RouteStepTranslatorTest()
    {
        var pickNodeProperty = new VehicleTypeNodeProperty(Guid.NewGuid(), _agv.VehicleTypeId,
        [
            new VehicleTypeNodePropertyAction(Vda5050Specifications.ActionTypes.Pick, BlockingType.HARD, requirementType: RequirementType.Required,
                actionParameters: [new NodeActionParameter(Vda5050Specifications.Parameters.StationType, "PICK")])
        ]);
        var startNode = new Node(StartNodeId, new NodePosition(0, 0), []);
        var endNode = new Node(EndNodeId, new NodePosition(1, 1), [pickNodeProperty]);
        var edge = new Edge(EdgeId, EndNodeId, StartNodeId, [new VehicleTypeEdgeProperty(Guid.NewGuid(), _agv.VehicleTypeId)]);
        var layout = new Layout(Guid.NewGuid(), "layout-1", "1.0", [edge], [startNode, endNode], []);

        _agvRepository.Setup(x => x.GetRequiredAgvAsync(_agv.CorrelationId)).ReturnsAsync(_agv);
        _lifRepository.Setup(x => x.GetLayoutsByVehicleTypeIdAsync(_agv.VehicleTypeId)).ReturnsAsync([layout]);
        _agvActionRepository.Setup(x => x.GetActionsForScopeAsync(_agv.SerialNumber, ActionScope.Node)).ReturnsAsync(
        [
            AgvGenerator.GenerateNodeAction(agvSerialNumber: _agv.SerialNumber,
                parameters: [new AgvActionParameter { Key = Vda5050Specifications.Parameters.StationType, IsOptional = true }])
        ]);

        var routeActionMapper = new RouteActionMapper(_agvActionRepository.Object, new PickOrderActionBuilder(), new DropOrderActionBuilder(), new StartChargingOrderActionBuilder());
        // Test edges/nodes never set OrientationType/Trajectory, so a mapper that maps null to null suffices —
        // avoids exercising OrderProfile's (pre-existing, unrelated) AssertConfigurationIsValid gaps.
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<OrientationType?>(It.IsAny<object>())).Returns((OrientationType?)null);
        mapper.Setup(m => m.Map<OrderTrajectory?>(It.IsAny<object>())).Returns((OrderTrajectory?)null);
        _translator = new RouteStepTranslator(_agvRepository.Object, _agvActionRepository.Object, _lifRepository.Object, new OrderActionBuilderRegistry([]), routeActionMapper, mapper.Object);
    }

    private static RouteStepExecution CreateTravelStep(bool released = false, PickAction? action = null)
    {
        return new RouteStepExecution(Guid.NewGuid(), StartNodeId.ToGuid(), EdgeId.ToGuid(), EndNodeId.ToGuid(), action) { Released = released };
    }

    private static RouteStepExecution CreateStationaryActionStep(bool released, PickAction action)
    {
        return new RouteStepExecution(Guid.NewGuid(), EndNodeId.ToGuid(), null, EndNodeId.ToGuid(), action) { Released = released };
    }

    [TestMethod]
    public async Task Translate_SingleTravelStepWithAction_ProducesOneNodeOneEdge()
    {
        var route = await _translator.Translate(_agv.CorrelationId, [CreateTravelStep(action: new PickAction(null))]);

        route.Nodes.Count.ShouldBe(2);
        route.Edges.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Translate_SingleTravelStepWithAction_AttachesActionToEndNode()
    {
        var route = await _translator.Translate(_agv.CorrelationId, [CreateTravelStep(action: new PickAction(null))]);

        var endNode = route.Nodes.Single(n => n.NodeId == EndNodeId);
        endNode.Actions.ShouldContain(a => a.ActionType == Vda5050Specifications.ActionTypes.Pick);
    }

    [TestMethod]
    public async Task Translate_ChainedTravelThenStationaryAction_DoesNotCreateASecondNode()
    {
        RouteStepExecution[] steps =
        [
            CreateTravelStep(),
            CreateStationaryActionStep(false, new PickAction(null))
        ];

        var route = await _translator.Translate(_agv.CorrelationId, steps);

        route.Nodes.Count.ShouldBe(2); // seed node + one merged node, not three
    }

    [TestMethod]
    public async Task Translate_ChainedTravelThenStationaryAction_MergesActionOntoTheTravelStepsNode()
    {
        RouteStepExecution[] steps =
        [
            CreateTravelStep(),
            CreateStationaryActionStep(false, new PickAction(null))
        ];

        var route = await _translator.Translate(_agv.CorrelationId, steps);

        var endNode = route.Nodes.Single(n => n.NodeId == EndNodeId);
        endNode.Actions.ShouldContain(a => a.ActionType == Vda5050Specifications.ActionTypes.Pick);
    }

    [TestMethod]
    public async Task Translate_ChainedTravelThenStationaryAction_StationaryStepSharesTravelStepsNodeSequenceId()
    {
        var travelStep = CreateTravelStep();
        var stationaryStep = CreateStationaryActionStep(false, new PickAction(null));

        await _translator.Translate(_agv.CorrelationId, [travelStep, stationaryStep]);

        stationaryStep.NodeSequenceId.ShouldBe(travelStep.NodeSequenceId);
        stationaryStep.EdgeSequenceId.ShouldBeNull();
    }

    [TestMethod]
    public async Task Translate_ReleasedStep_ProducesReleasedNodeAndEdge()
    {
        var route = await _translator.Translate(_agv.CorrelationId, [CreateTravelStep(released: true)]);

        route.Edges.Single().Released.ShouldBeTrue();
        route.Nodes.Single(n => n.NodeId == EndNodeId).Released.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Translate_UnreleasedStep_ProducesUnreleasedNodeAndEdge()
    {
        var route = await _translator.Translate(_agv.CorrelationId, [CreateTravelStep(released: false)]);

        route.Edges.Single().Released.ShouldBeFalse();
        route.Nodes.Single(n => n.NodeId == EndNodeId).Released.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Translate_SeedNode_IsAlwaysReleased()
    {
        var route = await _translator.Translate(_agv.CorrelationId, [CreateTravelStep(released: false)]);

        route.Nodes.Single(n => n.NodeId == StartNodeId).Released.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Translate_MutatesStepExecution_WithSequenceIdsAndVdaActionId()
    {
        var step = CreateTravelStep(action: new PickAction(null));

        await _translator.Translate(_agv.CorrelationId, [step]);

        step.NodeSequenceId.ShouldBe(2);
        step.EdgeSequenceId.ShouldBe(1);
        step.VdaActionId.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Translate_UnknownEndNode_ThrowsNodeNotDefinedForAgvException()
    {
        var step = new RouteStepExecution(Guid.NewGuid(), StartNodeId.ToGuid(), EdgeId.ToGuid(), "unknown".ToGuid(), null);

        await Should.ThrowAsync<NodeNotDefinedForAgvException>(() => _translator.Translate(_agv.CorrelationId, [step]));
    }
}
