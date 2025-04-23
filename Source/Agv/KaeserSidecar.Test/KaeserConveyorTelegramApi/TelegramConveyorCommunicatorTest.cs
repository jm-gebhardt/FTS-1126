using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

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
}