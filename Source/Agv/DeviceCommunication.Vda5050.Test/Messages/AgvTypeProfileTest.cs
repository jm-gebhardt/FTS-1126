using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Messages;

[TestClass]
public class AgvTypeProfileTest
{
    [TestMethod]
    public void Map_MqttFactsheetToAgvType_MarksAgvTypeAsReportedByAgv()
    {
        var fixture = new AgvTypeProfileTestFixture();
        var factsheet = MqttFactsheetGenerator.Generate(
            seriesName: AgvTypeProfileTestFixture.SeriesName,
            manufacturer: AgvTypeProfileTestFixture.Manufacturer);

        var agvType = fixture.Mapper.Map<AgvType>(factsheet);

        agvType.AgvTypeIdentifier.ShouldBe(AgvTypeProfileTestFixture.AgvTypeIdentifier);
        agvType.ReportedByAgv.ShouldBeTrue();
        agvType.Acknowledged.ShouldBeFalse();
    }
}