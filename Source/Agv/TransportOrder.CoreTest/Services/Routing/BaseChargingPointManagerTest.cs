using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing;

[TestClass]
public class BaseChargingPointManagerTest
{
    [TestMethod]
    public async Task GetBestCharger_WhenFreeChargerNodesExists_ReturnFirstFreeChargerNode()
    {
        const string agvId = "AGV001";
        var correlationId = agvId.ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);
        var graph = fixture.GetGraphWhereNoOneUnderstandWhyWeHaveTwoDifferentGraphTypes(agvGraph);
        var agv = fixture.GetAgv(correlationId, agvId,"Idle","","",agvGraph);

        fixture.SetupGraphRegistry(graph);
        fixture.SetupNodeLocker(true, true, true);
        
        var bestCharger = await fixture.CreateBaseChargingPointManager().GetBestChargerAsync(agv);
        bestCharger.ShouldNotBeNull();
        bestCharger.State.ShouldBe(AgvGraph.NodeState.Free);
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);
    }

    [TestMethod]
    public async Task GetBestCharger_WhenFreeChargerNodesDoesNotExists_ReturnNull()
    {
        const string agvId = "AGV001";
        var correlationId = agvId.ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);
        var graph = fixture.GetGraphWhereNoOneUnderstandWhyWeHaveTwoDifferentGraphTypes(agvGraph);
        var agv = fixture.GetAgv(correlationId, agvId,"Idle","","",agvGraph);

        fixture.SetupGraphRegistry(graph);
        fixture.SetupNodeLocker(true, true, true);
        
        var bestCharger = await fixture.CreateBaseChargingPointManager().GetBestChargerAsync(agv);
        bestCharger.ShouldBeNull();
    }

    [TestMethod]
    public async Task ReserveCharger_WhenNodeIsChargerTypeAndFree_ReturnTrue()
    {
        const string agvId = "AGV001";
        var correlationId = agvId.ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);
        var graph = fixture.GetGraphWhereNoOneUnderstandWhyWeHaveTwoDifferentGraphTypes(agvGraph);
        var agv = fixture.GetAgv(correlationId, agvId,"Idle","","",agvGraph);

        fixture.SetupGraphRegistry(graph);
        fixture.SetupNodeLocker(true, true, true);
        
        var baseChargingPointManager = fixture.CreateBaseChargingPointManager();
        var bestCharger = await baseChargingPointManager.GetBestChargerAsync(agv);
        bestCharger.ShouldNotBeNull();
        bestCharger.State.ShouldBe(AgvGraph.NodeState.Free);
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);
        
        await baseChargingPointManager.ReserveChargerAsync(bestCharger, correlationId);
        fixture.NodeStateRegistry.Verify(x => x.ReserveNodeAsync(bestCharger.Id, correlationId), Times.Once);
    }

    [TestMethod]
    public async Task ReserveCharger_WhenNodeIsNotChargerTypeAndFree_ReturnFalse()
    {
        var correlationId = "AGV001".ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);
        var graph = fixture.GetGraphWhereNoOneUnderstandWhyWeHaveTwoDifferentGraphTypes(agvGraph);

        var notChargerNode = agvGraph.Nodes.FirstOrDefault(node => node.Type != AgvGraph.NodeType.Charge);
        notChargerNode.ShouldNotBeNull();
        notChargerNode.State.ShouldBe(AgvGraph.NodeState.Free);
        notChargerNode.Type.ShouldNotBe(AgvGraph.NodeType.Charge);

        fixture.SetupGraphRegistry(graph);
        fixture.SetupNodeLocker(true, true, true);
        
        await fixture.CreateBaseChargingPointManager().ReserveChargerAsync(notChargerNode, correlationId);
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenFreeChargerExist_ReturnChargerNodeInReservedState()
    {
        const string agvId = "AGV001";
        var correlationId = agvId.ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);
        var graph = fixture.GetGraphWhereNoOneUnderstandWhyWeHaveTwoDifferentGraphTypes(agvGraph);
        var agv = fixture.GetAgv(correlationId, agvId,"Idle","","",agvGraph);

        fixture.SetupGraphRegistry(graph);
        fixture.SetupNodeLocker(true, true, true);
        
        var agvNodeResult = await fixture.CreateBaseChargingPointManager().ReserveChargerAsync(agv);
        agvNodeResult.ShouldNotBeNull();
        fixture.NodeStateRegistry.Verify(r => r.ReserveNodeAsync(agvNodeResult.Id,correlationId), Times.Once);
        agvNodeResult.Type.ShouldBe(AgvGraph.NodeType.Charge);
    }

    [TestMethod]
    public async Task FindAndReserveFreeCharger_WhenNoFreeChargerExist_ThrowsException()
    {
        const string agvId = "AGV001";
        var correlationId = agvId.ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);
        var graph = fixture.GetGraphWhereNoOneUnderstandWhyWeHaveTwoDifferentGraphTypes(agvGraph);
        var agv = fixture.GetAgv(correlationId, agvId,"Idle","","",agvGraph);
        
        fixture.SetupGraphRegistry(graph);
        fixture.SetupNodeLocker(true, true, true);
        
        await Should.ThrowAsync<Exception>(async () => await fixture.CreateBaseChargingPointManager().ReserveChargerAsync(agv));
    }
}