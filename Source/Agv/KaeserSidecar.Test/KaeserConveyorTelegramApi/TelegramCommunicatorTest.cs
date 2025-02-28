using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

[TestClass]
public class TelegramCommunicatorTest
{
    [TestMethod]
    public void ReportPalletPickDropStation_ValidData_AgvWeStationEnqueued()
    {
        var fixture = new TelegramConveyorCommunicatorTestFixture().Build(ServiceKeyProvider.AplWeStation);

        const string targetStation = "WE01";
        const string pickStation = "1040";
        const string dropStation = "1060";

        fixture.Processor.HasSendDemand().ShouldBeFalse();

        fixture.Communicator.ReportPalletPickDropStation(targetStation, pickStation, dropStation);

        fixture.Processor.HasSendDemand().ShouldBeTrue();
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        var agvWeStation = new AgvWeStation("any", "any", "any", DateTime.UtcNow);
        agvWeStation.Load(nextTelegram);

        agvWeStation.Drop.ShouldBe(dropStation);
        agvWeStation.Station.ShouldBe(targetStation);
        agvWeStation.Pick.ShouldBe(pickStation);
    }
}