using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class TelegramHostCommunicatorTest
{
    [TestMethod]
    public async Task ReportOrderCompletion_ValidData_EnqueuesArrivalTelegram()
    {
        var fixture = new TelegramHostCommunicatorTestFixture().Build();

        const string hu = "hu";
        const string destination = "1040";
        
        fixture.Processor.HasSendDemand().ShouldBeFalse();

        await fixture.Communicator.ReportOrderCompletion(hu, "orderId", destination);

        fixture.Processor.HasSendDemand().ShouldBeTrue();
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        var arrival = new Arrival(DateTime.UtcNow, "", "");
        arrival.Load(nextTelegram);

        arrival.Hu.ShouldBe(hu);
        arrival.Pos.ShouldBe(destination);
    }

    [TestMethod]
    public void ReportPickCompletion_ValidData_EnqueuesPickTelegram()
    {
        var fixture = new TelegramHostCommunicatorTestFixture().Build();

        const string hu = "hu";
        const string position = "1040";

        fixture.Processor.HasSendDemand().ShouldBeFalse();

        fixture.Communicator.ReportPickCompletion(hu, position, DateTime.UtcNow);

        fixture.Processor.HasSendDemand().ShouldBeTrue();
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        var pick = new Pick("", "", DateTime.UtcNow);
        pick.Load(nextTelegram);

        pick.Hu.ShouldBe(hu);
        pick.Pos.ShouldBe(position);
    }
}