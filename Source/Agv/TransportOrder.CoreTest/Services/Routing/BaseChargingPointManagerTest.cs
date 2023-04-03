using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing;

[TestClass]
public class BaseChargingPointManagerTest
{
    [TestMethod]
    public void GetBestCharger_WhenFreeChargerNodesExists_ReturnFirstFreeChargerNode()
    {
        var correlationId = "AGV001".ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);

        var bestCharger = fixture.GetBaseChargingPointManager().GetBestCharger(agvGraph);
        bestCharger.ShouldNotBeNull();
        bestCharger.State.ShouldBe(AgvGraph.NodeState.Free);
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);
    }

    [TestMethod]
    public void GetBestCharger_WhenFreeChargerNodesDoesNotExists_ReturnNull()
    {
        var correlationId = Guid.NewGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);

        var bestCharger = fixture.GetBaseChargingPointManager().GetBestCharger(agvGraph);
        bestCharger.ShouldBeNull();
    }

    [TestMethod]
    public void ReserveCharger_WhenNodeIsChargerTypeAndFree_ReturnTrue()
    {
        var correlationId = "AGV001".ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);

        var baseChargingPointManager = fixture.GetBaseChargingPointManager();
        var bestCharger = baseChargingPointManager.GetBestCharger(agvGraph);
        bestCharger.ShouldNotBeNull();
        bestCharger.State.ShouldBe(AgvGraph.NodeState.Free);
        bestCharger.Type.ShouldBe(AgvGraph.NodeType.Charge);

        bool reserveResult = baseChargingPointManager.ReserveCharger(bestCharger);
        bestCharger.State.ShouldBe(AgvGraph.NodeState.Reserved);
        reserveResult.ShouldBeTrue();
    }
    
    [TestMethod]
    public void ReserveCharger_WhenNodeIsNotChargerTypeAndFree_ReturnFalse()
    {
        var correlationId = "AGV001".ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);

        var notChargerNode = agvGraph.Nodes.FirstOrDefault(node => node.Type != AgvGraph.NodeType.Charge);
        notChargerNode.ShouldNotBeNull();
        notChargerNode.State.ShouldBe(AgvGraph.NodeState.Free);
        notChargerNode.Type.ShouldNotBe(AgvGraph.NodeType.Charge);

        bool reserveResult = fixture.GetBaseChargingPointManager().ReserveCharger(notChargerNode);
        reserveResult.ShouldBeFalse();
    }

    [TestMethod]
    public void FindAndReserveFreeCharger_WhenFreeChargerExist_ReturnChargerNodeInReservedState()
    {
        var correlationId = "AGV001".ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(true, correlationId);

        var agvNodeResult = fixture.GetBaseChargingPointManager().ReserveBestCharger(agvGraph);
        agvNodeResult.ShouldNotBeNull();
        agvNodeResult.State.ShouldBe(AgvGraph.NodeState.Reserved);
        agvNodeResult.Type.ShouldBe(AgvGraph.NodeType.Charge);
    }

    [TestMethod]
    public void FindAndReserveFreeCharger_WhenNoFreeChargerExist_ThrowsException()
    {
        var correlationId = "AGV001".ToGuid();
        var fixture = new BaseChargingPointManagerTestFixture();
        var agvGraph = fixture.GetAgvGraph(false, correlationId);

        Should.Throw<Exception>(() => fixture.GetBaseChargingPointManager().ReserveBestCharger(agvGraph));
    }
}