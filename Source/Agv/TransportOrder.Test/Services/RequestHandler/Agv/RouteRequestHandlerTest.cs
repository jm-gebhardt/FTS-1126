using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.Agv;

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

        await fixture.PublishMessageAsync(new SendAgvToCharger(agv.CorrelationId));

        var nodeStateChanged = await fixture.GetFirstPublishedMessageAsync<NodeStateChanged>();
        nodeStateChanged.NodeState.NodeId.ShouldBe(chargingNode.Id);
        nodeStateChanged.NodeState.NodeState.ShouldBe(NodeLockState.Reserved);
    }

    [TestMethod]
    public async Task Consume_SendAgvToCharger_AgvWithAction_PublishesStartChargingActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var chargingNode = NodeGenerator.GenerateChargingNode();
        var fixture = new SendAgvToChargerRequestHandlerTestFixture()
            .WithAgv(agv)
            .WithCurrentAction(null)
            .ReserveChargerReturns(chargingNode)
            .WithNodeLock(NodeLockGenerator.GenerateNodeLock(nodeId: chargingNode.Id, nodeLockState: NodeLockState.Reserved));

        await fixture.PublishMessageAsync(new SendAgvToCharger(agv.CorrelationId));

        var startChargingActionGroup = await fixture.GetFirstPublishedMessageAsync<StartChargingActionGroup>();
        startChargingActionGroup.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        startChargingActionGroup.ChargeStationId.ShouldBe(chargingNode.Id);
        startChargingActionGroup.Creator.ShouldBe(ActionGroupCreator.TransportOrderWebApi);
    }
}