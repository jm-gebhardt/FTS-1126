using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        var busFixture = new MassTransitConsumerTestFixture<SuccessResponseConsumer<QueueActionGroup>>();
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .WithNodeState(null)
            .BuildAsync(busFixture.TestHarness.Bus);

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi));
        (await busFixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetNodeNeedsReservation_Responds_AgvMoveSuccessfulReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var targetNode = new GraphNode("target") { Type = AgvGraph.NodeType.Park };
        var busFixture = new MassTransitConsumerTestFixture<SuccessResponseConsumer<QueueActionGroup>>();
        MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<MoveAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(targetNode)
            .WithNodeState(new NodeLock(targetNode.Id, NodeLockState.Free, false, null, DateTime.UtcNow))
            .BuildAsync(busFixture.TestHarness.Bus);

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new MoveAgv(agv.CorrelationId, targetNode.Id, ActionGroupCreator.TransportOrderWebApi));
        (await busFixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
        fixture.VerifyNodeWasReserved(targetNode.Id, agv.CorrelationId);
        (await busFixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
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

    [DataTestMethod]
    [DataRow(NodeLockState.Occupied)]
    [DataRow(NodeLockState.Reserved)]
    public async Task Consume_ChargeAgv_ChargerNodeFree_ThrowsNoNodesAvailableException(NodeLockState state)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var charger = new GraphNode("charger") { Type = AgvGraph.NodeType.Charge };
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .WithNodeState(new NodeLock(charger.Id, state, false, null, DateTime.UtcNow))
            .BuildAsync();

        await MediatorShould.ThrowRequestExceptionAsync<NoNodesAvailableException>(async () => await fixture.GetResponseAsync<ChargeAgv>(
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
        var busFixture = new MassTransitConsumerTestFixture<SuccessResponseConsumer<QueueActionGroup>>();
        MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler> fixture = new MoveAgvRequestHandlerTestFixture<ChargeAgv, MoveAgvRequestHandler>()
            .AddAgvs([agv])
            .WithNode(charger)
            .WithNodeState(new NodeLock(charger.Id, NodeLockState.Free, false, null, DateTime.UtcNow))
            .BuildAsync(busFixture.TestHarness.Bus);

        await fixture.GetResponseAsync<AgvMoveSuccessful>(new ChargeAgv(agv.CorrelationId, charger.Id));
        (await busFixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
        fixture.VerifyNodeWasReserved(charger.Id, agv.CorrelationId);
        (await busFixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
    }
}