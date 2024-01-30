using Gebhardt.StoreWare.Agv.DeviceCommunication.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Extensions;

[TestClass]
public class AgvDataExtensionTest
{
    [TestMethod]
    public void SetSimulationSpeed_ValidData_VelocitiesAndAccelerationUpdated()
    {
        var agv = new KarisType1(Guid.NewGuid(), "ip address");
        const int factor = 2;
        var updatedAgv = agv.SetSimulationSpeed(factor);
        updatedAgv.Acceleration.ShouldBe(agv.Acceleration * factor);
        updatedAgv.MaxVelocityTurn.ShouldBe(agv.MaxVelocityTurn * factor);
        updatedAgv.MaxVelocityTurnOnSpot.ShouldBe(agv.MaxVelocityTurnOnSpot * factor);
        updatedAgv.MaxVelocityStraight.ShouldBe(agv.MaxVelocityStraight * factor);
        updatedAgv.Model.ShouldBe(agv.Model);
        updatedAgv.Length.ShouldBe(agv.Length);
        updatedAgv.Payload.ShouldBe(agv.Payload);
        updatedAgv.Sensors.ShouldBe(agv.Sensors);
        updatedAgv.BatteryCapacity.ShouldBe(agv.BatteryCapacity);
        updatedAgv.CorrelationId.ShouldBe(agv.CorrelationId);
        updatedAgv.IpV4Address.ShouldBe(agv.IpV4Address);
        updatedAgv.Type.ShouldBe(agv.Type);
    }
}