using System.Text.Json;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Factsheets;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain;

[TestClass]
public class FactsheetSerializationTest
{    
    [TestMethod]
    public void SerializeDeserialize_Factsheet_RoundTrip()
    {
        var factsheet = new Factsheet
        {
            SerialNumber = "SN-001",
            Manufacturer = "Gebhardt",
            TypeSpecification = new TypeSpecification { SeriesName = "GTL", MaxLoadMass = 1500, NavigationTypes = [NavigationType.VirtualLineGuided, NavigationType.Autonomous] },
            PhysicalParameters = new PhysicalParameters
            {
                SpeedMax = 1.5,
                AccelerationMax = 0.8,
                Width = 0.8,
                Length = 1.2
            },
            VehicleConfig = new VehicleConfig
            {
                Versions = [new VehicleVersion { Key = "softwareVersion", Value = "1.2.3" }],
                Network = new NetworkConfig { LocalIpAddress = "192.168.1.100" }
            }
        };

        string jsonFactsheet = JsonSerializer.Serialize(factsheet);
        var copyFactsheet = JsonSerializer.Deserialize<Factsheet>(jsonFactsheet);

        copyFactsheet.ShouldNotBeNull();
        copyFactsheet.ShouldBeEquivalentTo(factsheet);
    }

    [TestMethod]
    public void Serialize_Factsheet_SnakeCaseNavigationType_IsMapped()
    {
        var factsheet = new Factsheet
        {
            SerialNumber = "SN-001",
            Manufacturer = "Gebhardt",
            TypeSpecification = new TypeSpecification { SeriesName = "GTL", MaxLoadMass = 1500, NavigationTypes = new[] { NavigationType.VirtualLineGuided, NavigationType.PhysicalLineGuided, NavigationType.Autonomous } },
            PhysicalParameters = new PhysicalParameters
            {
                SpeedMax = 1.5,
                AccelerationMax = 0.8,
                Width = 0.8,
                Length = 1.2
            },
            VehicleConfig = new VehicleConfig
            {
                Versions = [new VehicleVersion { Key = "softwareVersion", Value = "1.2.3" }],
                Network = new NetworkConfig { LocalIpAddress = "192.168.1.100" }
            }
        };
        string jsonFactsheet = JsonSerializer.Serialize(factsheet);

        jsonFactsheet.ShouldContain(@"""NavigationTypes"":[""VIRTUAL_LINE_GUIDED"",""PHYSICAL_LINE_GUIDED"",""AUTONOMOUS""]");
    }
    
    [TestMethod]
    public void Deserialize_Factsheet_SnakeCaseNavigationType_IsMapped()
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
        var factsheet = JsonSerializer.Deserialize<Factsheet>(jsonFactsheet);
        
        factsheet.ShouldNotBeNull();
        factsheet.TypeSpecification.NavigationTypes.Count.ShouldBe(3);
        factsheet.TypeSpecification.NavigationTypes.ShouldContain(NavigationType.VirtualLineGuided);
        factsheet.TypeSpecification.NavigationTypes.ShouldContain(NavigationType.PhysicalLineGuided);
        factsheet.TypeSpecification.NavigationTypes.ShouldContain(NavigationType.Autonomous);
    }
}