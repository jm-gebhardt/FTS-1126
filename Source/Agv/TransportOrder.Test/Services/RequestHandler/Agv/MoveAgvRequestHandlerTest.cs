using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class MoveAgvRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_MoveAgv_AgvDoesNotExist_ThrowsAgvNotFoundException()
    {
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<MoveAgv>(
            new MoveAgv("unknown agv".ToGuid(), "known target".ToGuid(), ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeDoesNotExist_ThrowsNodeNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode([null])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () => await fixture.GetResponseAsync<MoveAgv>(
            new MoveAgv(agv.CorrelationId, "unknown target".ToGuid(), ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_MoveAgv_AgvIsLocked_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () => await fixture.GetResponseAsync<MoveAgv>(
            new MoveAgv(agv.CorrelationId, "known target".ToGuid(), ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_MoveAgv_ValidData_Responds_AgvMoveSuccessful()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = NodeGenerator.GenerateNode(nodeName: "target");
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi, "user"));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeNeedsReservation_Responds_AgvMoveSuccessfulReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = NodeGenerator.GenerateNode(nodeName: "target", nodeType: AgvGraph.NodeType.Park);
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi, "user"));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyNodeWasReserved(AgvGraph.NodeType.Park, targetNode, agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeNeedsReservationAndHasAdditionalType_Responds_AgvMoveSuccessfulReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = NodeGenerator.GenerateNodeWithMultipleTypes([AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge], nodeId: "target");
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi, "user"));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyNodeWasReserved(AgvGraph.NodeType.Park, targetNode, agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeNeedsReservation_ButServiceThrows()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = NodeGenerator.GenerateNode(nodeName: "target", nodeType: AgvGraph.NodeType.Park);
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi, "user"));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyNodeWasReserved(AgvGraph.NodeType.Park, targetNode, agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_ParkReservationThrows_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var charger = NodeGenerator.GenerateNode(nodeName: "park", nodeType: AgvGraph.NodeType.Park);
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .BuildAsync();
        fixture.ParkPointService.Setup(x => x.ReserveParkAsync(It.IsAny<Guid>(), agv.CorrelationId, true)).ThrowsAsync(new ParkNodeNotAvailableException(charger.Id));

        await MediatorShould.ThrowRequestExceptionAsync<ParkNodeNotAvailableException>(async () => await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, charger.Id, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_AgvDoesNotExist_ThrowsAgvNotFoundException()
    {
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv("unknown agv".ToGuid(), "known charger".ToGuid(), "user")));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_ChargerDoesNotExist_ThrowsNodeNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode([null])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv(agv.CorrelationId, "unknown charger".ToGuid(), "user")));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_AgvIsLocked_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv(agv.CorrelationId, "known charger".ToGuid(), "user")));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_ChargeReservationThrows_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var charger = NodeGenerator.GenerateNode(nodeName: "charger", nodeType: AgvGraph.NodeType.Charge);
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .BuildAsync();
        fixture.ChargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<GraphNode>(), It.IsAny<Guid>(), It.IsAny<bool>())).ThrowsAsync(new ChargeStationNotAvailableException(""));

        await MediatorShould.ThrowRequestExceptionAsync<ChargeStationNotAvailableException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv(agv.CorrelationId, charger.Id, "user")));
    }

    [TestMethod]
    [DataRow(AgvGraph.NodeType.PickDrop)]
    [DataRow(AgvGraph.NodeType.Drop)]
    [DataRow(AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.WayPoint)]
    [DataRow(AgvGraph.NodeType.Park)]
    public async Task Consume_ChargeAgv_GivenNodeIsNotCharger_ThrowsInvalidNodeTypeException(AgvGraph.NodeType type)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var node = NodeGenerator.GenerateNode(nodeName: "charger", nodeType: type);
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(node)
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv(agv.CorrelationId, node.Id, "user")));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_ValidData_Responds_AgvMoveSuccessfulReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        var charger = NodeGenerator.GenerateNode(nodeName: "target", nodeType: AgvGraph.NodeType.Charge);
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new ChargeAgv(agv.CorrelationId, charger.Id, "user"));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyNodeWasReserved(AgvGraph.NodeType.Charge, charger, agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_AgvDoesNotExist_ThrowsAgvNotFoundException()
    {
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<MoveAgvToCoordinate>(
            new MoveAgvToCoordinate("unknown agv".ToGuid(), 1, 1, 0, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_AgvIsLocked_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () => await fixture.GetResponseAsync<MoveAgvToCoordinate>(
            new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_AgvIsInAutomaticMode_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () => await fixture.GetResponseAsync<MoveAgvToCoordinate>(
            new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_TargetPositionIsInRestrictedArea_ThrowsPositionIsInRestrictedAreaException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.PrepareMaintenanceMode(MaintenanceReason.Other, "Reason Description");
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithInsideRestrictedArea(true)
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<PositionIsInRestrictedAreaException>(async () => await fixture.GetResponseAsync<MoveAgvToCoordinate>(
            new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi, "user")));
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_ValidData_MaintenanceMode_RespondsAgvMoveSuccessfulPublishesQueueActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.PrepareMaintenanceMode(MaintenanceReason.Other, "Reason Description");
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithInsideRestrictedArea(false)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi, "user"));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_ValidData_UserMode_RespondsAgvMoveSuccessfulPublishesQueueActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.UserMode));
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithInsideRestrictedArea(false)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi, "user"));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }
}