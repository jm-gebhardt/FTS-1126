using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using static Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvGraph;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.States;

[TestClass]
public class SimulatedAgvSagaTest
{
    private SimulatedAgvSaga CreateSimulatedAgvSaga()
    {
        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();
        var graph = new GridGraphGenerator(new[] { "S1" }, new[] { "D1" }).Generate(correlationId);
        var data = new AgvData(correlationId, "model", "type", null, 10, 1, 1, 1, 1, 1, 1, 1, Array.Empty<AgvData.Sensor>());
        var map = new MapGenerator(graph);
        var agv = new SimulatedAgv(correlationId, agvId, "",
            true,
            false,
            false,
            true,
            DateTime.MinValue,
            DateTime.MinValue,
            AgvGraph: graph, AgvData: data, AgvMap: map.Generate());
        return new SimulatedAgvSaga(agv, 10, 75);
    }

    [TestMethod]
    public void Charging()
    {
        double chargeInit = 50;
        double chargeDiff = 0.1;
        double timeDiff = 60;

        var saga = CreateSimulatedAgvSaga();
        saga.SimulatedBatteryCharge = chargeInit;
        saga.CurrentNode = new GraphNode("node01") { Type = NodeType.Charge };

        saga.UpdateStatus(timeDiff, chargeDiff, chargeDiff);

        saga.SimulatedBatteryCharge.ShouldBe(chargeInit + chargeDiff * timeDiff);
        saga.AgvStatus.Charging.ShouldBeTrue();
    }

    [TestMethod]
    public void Discharging()
    {
        double chargeInit = 50;
        double chargeDiff = 0.1;
        double timeDiff = 60;

        var saga = CreateSimulatedAgvSaga();
        saga.SimulatedBatteryCharge = chargeInit;
        saga.CurrentNode = new GraphNode("node01") { Type = NodeType.Pick };

        saga.UpdateStatus(timeDiff, chargeDiff, chargeDiff);

        saga.SimulatedBatteryCharge.ShouldBe(chargeInit - chargeDiff * timeDiff);
        saga.AgvStatus.Charging.ShouldBeFalse();
    }

    [TestMethod]
    public void Not_alive_when_below_charge_threshold()
    {
        double chargeInit = 10;
        double chargeDiff = 0.1;
        double timeDiff = 60;

        var saga = CreateSimulatedAgvSaga();
        saga.SimulatedBatteryCharge = chargeInit;

        saga.IsOnline.ShouldBeTrue();

        saga.CurrentNode = new GraphNode("node01") { Type = NodeType.Pick };
        saga.UpdateStatus(timeDiff, chargeDiff, chargeDiff);

        saga.IsOnline.ShouldBeFalse();
    }
}