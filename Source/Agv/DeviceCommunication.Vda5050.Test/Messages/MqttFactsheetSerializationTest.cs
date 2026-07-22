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
        var mqttFactsheet = MqttFactsheetGenerator.Generate(
            actions: [MqttFactsheetGenerator.GenerateAgvAction()]);
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
        var factsheet = JsonSerializer.Deserialize<MqttFactsheet>(jsonFactsheet);
        
        factsheet.ShouldNotBeNull();
        factsheet.TypeSpecification.NavigationTypes.Count.ShouldBe(3);
        factsheet.TypeSpecification.NavigationTypes.ShouldContain(MqttNavigationType.VirtualLineGuided);
        factsheet.TypeSpecification.NavigationTypes.ShouldContain(MqttNavigationType.PhysicalLineGuided);
        factsheet.TypeSpecification.NavigationTypes.ShouldContain(MqttNavigationType.Autonomous);
    }
 
    [TestMethod]
    public void Serialize_MqttFactsheet_SnakeCaseNavigationType_IsMapped()
    {
        var mqttFactsheet = MqttFactsheetGenerator.Generate();
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