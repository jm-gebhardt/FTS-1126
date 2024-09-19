using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class RouteRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_SendAgvToCharger_IdleAgv_ReservesCharger()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string node = "intermediateNode";
        var currentAction = AgvActionGenerator.GenerateAction(targetNodeId: node);
        var fixture = new SendAgvToChargerRequestHandlerTestFixture()
            .WithAgv(agv)
            .WithCurrentAction(currentAction);

        await fixture.PublishMessageAsync(new SendAgvToCharger(agv.CorrelationId));

        fixture.ChargingPointManager.Verify(x => x.ReserveChargerAsync(agv, node), Times.Once);
    }

    [TestMethod]
    public async Task Consume_SendAgvToCharger_AgvWithAction_PublishesNodeStateChange()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var fixture = new SendAgvToChargerRequestHandlerTestFixture()
            .WithAgv(agv)
            .WithCurrentAction(null)
            .ReserveChargerReturns(chargingNode)
            .WithNodeLock(NodeLockGenerator.GenerateNodeLock(nodeId: chargingNode.Id, nodeLockState: NodeLockState.Reserved));

        var nodeStateChanged = await fixture.PublishAndGetFirstMessageAsync<SendAgvToCharger, NodeStateChanged>(
            new SendAgvToCharger(agv.CorrelationId));
        
        nodeStateChanged.NodeState.NodeId.ShouldBe(chargingNode.Id);
        nodeStateChanged.NodeState.NodeState.ShouldBe(NodeLockState.Reserved);
    }

    [TestMethod]
    public async Task Consume_SendAgvToCharger_AgvIsLocked_DoesNotPublishNodeStateChange()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.PrepareLockedMode();
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var fixture = new SendAgvToChargerRequestHandlerTestFixture()
            .WithAgv(agv)
            .WithCurrentAction(null)
            .ReserveChargerReturns(chargingNode)
            .WithNodeLock(NodeLockGenerator.GenerateNodeLock(nodeId: chargingNode.Id, nodeLockState: NodeLockState.Reserved));

        await fixture.PublishMessageAsync(new SendAgvToCharger(agv.CorrelationId));
        await fixture.ShouldHaveConsumedAsync<SendAgvToCharger>();
        await fixture.ShouldNotHaveConsumedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_SendAgvToCharger_AgvWithAction_PublishesCreateChargingActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var fixture = new SendAgvToChargerRequestHandlerTestFixture()
            .WithAgv(agv)
            .WithCurrentAction(null)
            .ReserveChargerReturns(chargingNode)
            .WithNodeLock(NodeLockGenerator.GenerateNodeLock(nodeId: chargingNode.Id, nodeLockState: NodeLockState.Reserved));

        var createChargingActionGroup = await fixture.PublishAndGetFirstMessageAsync<SendAgvToCharger, CreateChargingActionGroup>(
            new SendAgvToCharger(agv.CorrelationId));

        createChargingActionGroup.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        createChargingActionGroup.ChargeStationId.ShouldBe(chargingNode.Id);
        createChargingActionGroup.Creator.ShouldBe(ActionGroupCreator.TransportOrderWebApi);
    }
}