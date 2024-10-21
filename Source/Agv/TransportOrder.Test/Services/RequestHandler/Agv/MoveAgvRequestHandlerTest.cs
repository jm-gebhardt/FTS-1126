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
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            new MoveAgv("unknown agv".ToGuid(), "known target", ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeDoesNotExist_ThrowsNodeNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(null)
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () => await fixture.GetResponseAsync<MoveAgv>(
            new MoveAgv(agv.CorrelationId, "unknown target", ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_MoveAgv_AgvIsLocked_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () => await fixture.GetResponseAsync<MoveAgv>(
            new MoveAgv(agv.CorrelationId, "known target", ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_MoveAgv_ValidData_Responds_AgvMoveSuccessful()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = NodeGenerator.GenerateNode(nodeId:"target");
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeNeedsReservation_Responds_AgvMoveSuccessfulReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode =  NodeGenerator.GenerateNode(nodeId:"target", nodeType:AgvGraph.NodeType.Park);
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyNodeWasReserved(targetNode, agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeNeedsReservation_ButServiceThrows()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode =  NodeGenerator.GenerateNode(nodeId:"target", nodeType:AgvGraph.NodeType.Park);
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyNodeWasReserved(targetNode, agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_ParkReservationThrows_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var charger =  NodeGenerator.GenerateNode(nodeId:"park", nodeType:AgvGraph.NodeType.Park);
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .BuildAsync();
        fixture.ParkPointService.Setup(x => x.ReserveParkAsync(It.IsAny<string>(), agv.CorrelationId)).ThrowsAsync(new ParkNodeNotAvailableException(""));

        await MediatorShould.ThrowRequestExceptionAsync<ParkNodeNotAvailableException>(async () => await fixture.GetResponseAsync<MoveAgv>(
            new MoveAgv(agv.CorrelationId, "known target", ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_AgvDoesNotExist_ThrowsAgvNotFoundException()
    {
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv("unknown agv".ToGuid(), "known charger")));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_ChargerDoesNotExist_ThrowsNodeNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();

        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(null)
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv(agv.CorrelationId, "unknown charger")));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_AgvIsLocked_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv(agv.CorrelationId, "known charger")));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_ChargeReservationThrows_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var charger =  NodeGenerator.GenerateNode(nodeId:"charger", nodeType:AgvGraph.NodeType.Charge);
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .BuildAsync();
        fixture.ChargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<GraphNode>(), It.IsAny<Guid>())).ThrowsAsync(new ChargeStationNotAvailableException(""));

        await MediatorShould.ThrowRequestExceptionAsync<ChargeStationNotAvailableException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv(agv.CorrelationId, charger.Id)));
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.PickDrop)]
    [DataRow(AgvGraph.NodeType.Drop)]
    [DataRow(AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.WayPoint)]
    [DataRow(AgvGraph.NodeType.Park)]
    public async Task Consume_ChargeAgv_GivenNodeIsNotCharger_ThrowsInvalidNodeTypeException(AgvGraph.NodeType type)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var node =  NodeGenerator.GenerateNode(nodeId:"charger", nodeType:type);
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(node)
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
            new ChargeAgv(agv.CorrelationId, node.Id)));
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_ValidData_Responds_AgvMoveSuccessfulReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        
        var charger = NodeGenerator.GenerateNode(nodeId:"target", nodeType:AgvGraph.NodeType.Charge);
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new ChargeAgv(agv.CorrelationId, charger.Id));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyNodeWasReserved(charger, agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_AgvDoesNotExist_ThrowsAgvNotFoundException()
    {
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<MoveAgvToCoordinate>(
            new MoveAgvToCoordinate("unknown agv".ToGuid(), 1, 1, 0, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_AgvIsLocked_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () => await fixture.GetResponseAsync<MoveAgvToCoordinate>(
            new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi)));
    }

    [TestMethod]
    public async Task Consume_MoveAgvToCoordinate_AgvIsInAutomaticMode_ThrowsActionNotAllowedInModeException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgvToCoordinate, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<ActionNotAllowedInModeException>(async () => await fixture.GetResponseAsync<MoveAgvToCoordinate>(
            new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi)));
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
            new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi)));
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

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi));
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

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgvToCoordinate(agv.CorrelationId, 1, 1, 0, ActionGroupCreator.TransportOrderWebApi));
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }
}