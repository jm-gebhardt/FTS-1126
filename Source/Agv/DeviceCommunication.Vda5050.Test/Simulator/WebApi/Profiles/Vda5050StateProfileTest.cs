using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Profiles.Vda5050StateProfileTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Profiles;

[TestClass]
public class Vda5050StateProfileTest
{
    [TestMethod]
    public void Map_MqttStateToDto_MapsKeyFields()
    {
        var fixture = new Fixture();
        var state = MqttStateGenerator.Generate(
            Fixture.SerialNumber,
            Fixture.BatteryCharge,
            MqttOperatingMode.Manual,
            MqttStateGenerator.GenerateAgvPosition(Fixture.PositionX, theta: Fixture.Theta),
            [MqttStateGenerator.GenerateNodeState(Fixture.NodeId)]);

        var dto = fixture.Mapper.Map<Vda5050StateDto>(state);

        dto.ShouldSatisfyAllConditions(
            () => dto.SerialNumber.ShouldBe(Fixture.SerialNumber),
            () => dto.BatteryState.BatteryCharge.ShouldBe(Fixture.BatteryCharge),
            () => dto.OperatingMode.ShouldBe(OperatingModeDto.Manual),
            () => dto.AgvPosition.ShouldNotBeNull(),
            () => dto.AgvPosition!.X.ShouldBe(Fixture.PositionX),
            () => dto.AgvPosition!.Theta.ShouldBe(Fixture.Theta),
            () => dto.NodeStates.ShouldHaveSingleItem(),
            () => dto.NodeStates[0].NodeId.ShouldBe(Fixture.NodeId),
            () => dto.NodeStates[0].Released.ShouldBeTrue());
    }

    [TestMethod]
    public void Map_MqttStateToDto_MapsMessageCreatedDateFromTimestamp()
    {
        var fixture = new Fixture();
        var state = MqttStateGenerator.Generate();

        var dto = fixture.Mapper.Map<Vda5050StateDto>(state);

        dto.MessageCreatedDate.ShouldBe(state.Timestamp.UtcDateTime);
    }

    [TestMethod]
    public void Map_MqttStateToDto_DoesNotSetCorrelationId()
    {
        var fixture = new Fixture();
        var state = MqttStateGenerator.Generate();

        var dto = fixture.Mapper.Map<Vda5050StateDto>(state);

        dto.CorrelationId.ShouldBe(Guid.Empty);
    }
}