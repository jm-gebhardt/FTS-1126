using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Services;

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
        const string hu = "123";

        fixture.ToWePlcTelegramProcessor.HasSendDemand().ShouldBeFalse();

        fixture.Communicator.ReportPalletPickDropStation(targetStation, pickStation, dropStation, hu);

        fixture.ToWePlcTelegramProcessor.HasSendDemand().ShouldBeTrue();
        fixture.ToWePlcTelegramProcessor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        var agvWeStation = new AgvWeStation();
        agvWeStation.Load(nextTelegram);

        agvWeStation.Drop.ShouldBe(dropStation);
        agvWeStation.Station.ShouldBe(targetStation);
        agvWeStation.Pick.ShouldBe(pickStation);
        agvWeStation.Hu.ShouldBe(hu);
    }

    [TestMethod]
    public void RequestStationState_ValidData_PosStateReqEnqueued()
    {
        const string station = "PS01";

        var fixture = new TelegramConveyorCommunicatorTestFixture().Build(ServiceKeyProvider.AplWeStation);
        fixture.ToWePlcTelegramProcessor.HasSendDemand().ShouldBeFalse();

        fixture.Communicator.RequestStationState(station);

        fixture.ToWePlcTelegramProcessor.HasSendDemand().ShouldBeTrue();
        fixture.ToWePlcTelegramProcessor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        var postStateReq = new PosStateReq();
        postStateReq.Load(nextTelegram);
        postStateReq.Pos.ShouldBe(station);
    }
}