using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper.AgvActionGenerator;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.Metrics;

[TestClass]
public class PrometheusMetricCollectionServiceTest
{
    [TestMethod]
    public void CollectBatterChargeMetrics_ValidData_BatterChargeGaugeUpdated()
    {
        var fixture = new PrometheusMetricCollectionServiceTestFixture();
        const int batteryCharge = 40;
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: batteryCharge);

        fixture.Service.CollectBatteryChargeMetrics(agv.AgvStatus);
        PrometheusMetricCollector.BatteryChargeGauge.WithLabels(agv.CorrelationId.ToString()).Value.ShouldBe(batteryCharge);
    }

    [TestMethod]
    public async Task CollectOrderCompletionMetrics_PickDropOrder_TravelDistanceAndCompletionCountUpdated()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed, assignedAgv: agvId, source: AgvGraphGenerator.Src1, destination: AgvGraphGenerator.Dst1);
        actionGroup.Actions.First().Nodes = [AgvGraphGenerator.Prk1, AgvGraphGenerator.Way1, AgvGraphGenerator.Src1];
        actionGroup.Actions.Last().Nodes = [AgvGraphGenerator.Src1, AgvGraphGenerator.Way1, AgvGraphGenerator.Way3, AgvGraphGenerator.Dst1];
        var fixture = new PrometheusMetricCollectionServiceTestFixture().AddGraph(graph);

        await fixture.Service.CollectOrderCompletionMetrics(actionGroup);

        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(agvId.ToString()).Value.ShouldBe(1);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBeGreaterThan(0);
    }
}