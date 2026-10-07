using System.Text.Json;
using MassTransit.Serialization;
using Shouldly;
using DeviceCommunicationFactsheet = Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt.MqttFactsheet;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Messages.MqttFactsheetSerializationTestFixture;
using SimulatorFactsheet = Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt.MqttFactsheet;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Messages;

[TestClass]
public class MqttFactsheetSerializationTest
{
    [TestMethod]
    public void Serialize_UploadedFactsheetWithSimulatorHeader_IsReadableByDeviceCommunication()
    {
        var fixture = new Fixture();
        string uploadedJson = JsonSerializer.Serialize(fixture.UploadedFactsheet, SystemTextJsonMessageSerializer.Options);
        var simulatorFactsheet = JsonSerializer.Deserialize<SimulatorFactsheet>(uploadedJson, SystemTextJsonMessageSerializer.Options).ShouldNotBeNull();
        simulatorFactsheet.HeaderId = Fixture.HeaderId;
        simulatorFactsheet.SerialNumber = Fixture.SimulatedSerialNumber;

        string publishedJson = JsonSerializer.Serialize(simulatorFactsheet, SystemTextJsonMessageSerializer.Options);

        var received = JsonSerializer.Deserialize<DeviceCommunicationFactsheet>(publishedJson, SystemTextJsonMessageSerializer.Options).ShouldNotBeNull();
        received.HeaderId.ShouldBe(Fixture.HeaderId);
        received.SerialNumber.ShouldBe(Fixture.SimulatedSerialNumber);
        received.Manufacturer.ShouldBe(Fixture.Manufacturer);
        received.TypeSpecification.ShouldBeEquivalentTo(fixture.UploadedFactsheet.TypeSpecification);
        received.PhysicalParameters.ShouldBeEquivalentTo(fixture.UploadedFactsheet.PhysicalParameters);
        received.ProtocolFeatures.ShouldBeEquivalentTo(fixture.UploadedFactsheet.ProtocolFeatures);
    }

    [TestMethod]
    public void Serialize_SimulatorFactsheet_WritesContentAsNamedTopLevelProperties()
    {
        var fixture = new Fixture();
        string uploadedJson = JsonSerializer.Serialize(fixture.UploadedFactsheet, SystemTextJsonMessageSerializer.Options);
        var simulatorFactsheet = JsonSerializer.Deserialize<SimulatorFactsheet>(uploadedJson, SystemTextJsonMessageSerializer.Options).ShouldNotBeNull();

        string publishedJson = JsonSerializer.Serialize(simulatorFactsheet, SystemTextJsonMessageSerializer.Options);

        using var document = JsonDocument.Parse(publishedJson);
        document.RootElement.GetProperty(Fixture.TypeSpecificationPropertyName).ValueKind.ShouldBe(JsonValueKind.Object);
    }
}