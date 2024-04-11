using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

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
    public async Task Consume_MoveAgv_ValidData_Responds_AgvMoveSuccessful()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = new GraphNode("target");
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi));
        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeNeedsReservation_Responds_AgvMoveSuccessfulReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = new GraphNode("target") { Type = AgvGraph.NodeType.Park };
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi));
        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
        fixture.VerifyNodeWasReserved(targetNode, agv.CorrelationId);
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeNeedsReservation_ButServiceThrows()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = new GraphNode("target") { Type = AgvGraph.NodeType.Park };
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi));
        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
        fixture.VerifyNodeWasReserved(targetNode, agv.CorrelationId);
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ChargeAgv_ParkReservationThrows_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var charger = new GraphNode("park") { Type = AgvGraph.NodeType.Park };
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
    public async Task Consume_ChargeAgv_ChargeReservationThrows_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var charger = new GraphNode("charger") { Type = AgvGraph.NodeType.Charge };
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
        var node = new GraphNode("charger") { Type = type };
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
        var charger = new GraphNode("target") { Type = AgvGraph.NodeType.Charge };
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .BuildAsync();

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new ChargeAgv(agv.CorrelationId, charger.Id));
        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
        fixture.VerifyNodeWasReserved(charger, agv.CorrelationId);
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
    }
}