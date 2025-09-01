using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Shouldly;
using PosStateReq = Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams.PosStateReq;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi.Services;

[TestClass]
public class TelegramConveyorCommunicatorTest
{
    [TestMethod]
    public void ReportAplCargoState_ValidData_AgvAplCargoStateEnqueued()
    {
        var fixture = new TelegramConveyorCommunicatorTestFixture().Build(ServiceKeyProvider.AgvCargoState);

        const bool cargo = true;

        fixture.AplCargoStateProcessor.HasSendDemand().ShouldBeFalse();

        fixture.Communicator.ReportAplCargoState(cargo);

        fixture.AplCargoStateProcessor.HasSendDemand().ShouldBeTrue();
        fixture.AplCargoStateProcessor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        var agvAplCargoState = new AgvAplCargoState(false, DateTime.UtcNow);
        agvAplCargoState.Load(nextTelegram);

        agvAplCargoState.Cargo.ShouldBe(cargo);
    }

    [TestMethod]
    public void RequestStationState_FromWeStationConnectionUsed_PosStateReqEnqueued()
    {
        var fixture = new TelegramConveyorCommunicatorTestFixture().Build(ServiceKeyProvider.AplWeStation);
        fixture.ToWePlcTelegramProcessor.HasSendDemand().ShouldBeFalse();
        const string station = "PS01";

        fixture.Communicator.RequestStationState(station);

        fixture.ToWePlcTelegramProcessor.HasSendDemand().ShouldBeTrue();
        fixture.ToWePlcTelegramProcessor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        var posStateReq = new PosStateReq();
        posStateReq.Load(nextTelegram);
        posStateReq.Pos.ShouldBe(station);
    }

    [TestMethod]
    public void RequestStationState_FromWeStationConnectionNotUsed_PosStateReqNotEnqueued()
    {
        var fixture = new TelegramConveyorCommunicatorTestFixture()
            .Build(ServiceKeyProvider.AplWeStation)
            .DisableFromWePlcConnection();
        fixture.ToWePlcTelegramProcessor.HasSendDemand().ShouldBeFalse();
       
        fixture.Communicator.RequestStationState("PS01");

        fixture.ToWePlcTelegramProcessor.HasSendDemand().ShouldBeFalse();
    }
}