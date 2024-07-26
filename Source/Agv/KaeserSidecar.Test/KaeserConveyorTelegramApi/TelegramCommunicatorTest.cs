using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

[TestClass]
public class TelegramCommunicatorTest
{
    [TestMethod]
    public void ReportPalletPickDropStation_ValidData_AgvWeStationEnqueued()
    {
        var fixture = new TelegramConveyorCommunicatorTestFixture().Build();

        const string targetStation = "WE01";
        const bool hasCargo = false;
        const string pickStation = "1040";

        fixture.Processor.HasSendDemand().ShouldBeFalse();

        fixture.Communicator.ReportPalletPickDropStation(targetStation, hasCargo, pickStation);

        fixture.Processor.HasSendDemand().ShouldBeTrue();
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        var agvWeStation = new AgvWeStation("any", true, "any", DateTime.UtcNow);
        agvWeStation.Load(nextTelegram);

        agvWeStation.Cargo.ShouldBe(hasCargo);
        agvWeStation.Station.ShouldBe(targetStation);
        agvWeStation.Pick.ShouldBe(pickStation);
    }
}