using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;
using DeviceCommunicationFactsheet = Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt.MqttFactsheet;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Messages;

public class MqttFactsheetSerializationTestFixture
{
    private const string UploadedSerialNumber = "uploaded01";
    public const string SimulatedSerialNumber = "simagv0";
    public const string Manufacturer = "gebhardt";
    private const string SeriesName = "simagv";
    public const string TypeSpecificationPropertyName = "typeSpecification";
    public const int HeaderId = 7;

    public DeviceCommunicationFactsheet UploadedFactsheet { get; } = MqttFactsheetGenerator.Generate(
        UploadedSerialNumber,
        [MqttFactsheetGenerator.GenerateAgvAction()],
        SeriesName,
        manufacturer: Manufacturer);
}