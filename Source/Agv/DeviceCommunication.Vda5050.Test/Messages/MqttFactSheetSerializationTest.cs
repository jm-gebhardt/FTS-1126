using System.Text.Json;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Messages;

[TestClass]
public class MqttFactsheetSerializationTest
{
    [TestMethod]
    public void SerializeDeserialize_MqttFactsheet_RoundTrip()
    {
        var mqttFactsheet = MqttFactSheetGenerator.Generate(
            actions: [MqttFactSheetGenerator.GenerateAgvAction()]);
        mqttFactsheet.TypeSpecification.NavigationTypes = [MqttNavigationType.VirtualLineGuided];

        string jsonFactsheet = JsonSerializer.Serialize(mqttFactsheet);
        var copyMqttFactsheet = JsonSerializer.Deserialize<MqttFactsheet>(jsonFactsheet);

        copyMqttFactsheet.ShouldNotBeNull();
        copyMqttFactsheet.ShouldBeEquivalentTo(mqttFactsheet);
    }

    [TestMethod]
    public void Deserialize_MqttFactsheet_SnakeCaseNavigationType_IsMapped()
    {
        const string jsonFactsheet = """
                                     {  
                                       "SerialNumber": "agv01",
                                       "TypeSpecification": {
                                         "SeriesName": "series",
                                         "NavigationTypes": [ "VIRTUAL_LINE_GUIDED", "PHYSICAL_LINE_GUIDED", "AUTONOMOUS" ]
                                       }
                                     }
                                     """;
        var factSheet = JsonSerializer.Deserialize<MqttFactsheet>(jsonFactsheet);
        
        factSheet.ShouldNotBeNull();
        factSheet.TypeSpecification.NavigationTypes.Count.ShouldBe(3);
        factSheet.TypeSpecification.NavigationTypes.ShouldContain(MqttNavigationType.VirtualLineGuided);
        factSheet.TypeSpecification.NavigationTypes.ShouldContain(MqttNavigationType.PhysicalLineGuided);
        factSheet.TypeSpecification.NavigationTypes.ShouldContain(MqttNavigationType.Autonomous);
    }
 
    [TestMethod]
    public void Serialize_MqttFactsheet_SnakeCaseNavigationType_IsMapped()
    {
        var mqttFactsheet = MqttFactSheetGenerator.Generate();
        mqttFactsheet.TypeSpecification.NavigationTypes = [MqttNavigationType.VirtualLineGuided, MqttNavigationType.PhysicalLineGuided, MqttNavigationType.Autonomous];

        string jsonFactsheet = JsonSerializer.Serialize(mqttFactsheet);

        jsonFactsheet.ShouldContain(@"""NavigationTypes"":[""VIRTUAL_LINE_GUIDED"",""PHYSICAL_LINE_GUIDED"",""AUTONOMOUS""]");
    }
    
    [TestMethod]
    public void Deserialize_MqttFactsheet_InvalidNavigationType_JsonExceptionThrown()
    {
        const string json = """
                            {  
                              "SerialNumber": "agv01",
                              "TypeSpecification": {
                                "SeriesName": "series",
                                "NavigationTypes": [ "INVALID_LINE_GUIDED", "PHYSICAL_LINE_GUIDED", "AUTONOMOUS" ]
                              }
                            }
                            """;
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<MqttFactsheet>(json));
    }
}