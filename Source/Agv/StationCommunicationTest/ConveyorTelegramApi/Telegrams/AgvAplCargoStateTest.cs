using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Telegrams;

[TestClass]
public class AgvAplCargoStateTest
{
    [TestMethod]
    public void Construct_ValidData_AllPropertiesSet()
    {
        const bool cargo = true;
        var time = DateTime.UtcNow;

        var telegram = new AgvAplCargoState(cargo, time);

        telegram.Func.ShouldBe(nameof(AgvAplCargoState));
        telegram.Cargo.ShouldBe(cargo);
        telegram.Time.ShouldBe(time);
        telegram.Send.ShouldBe("FTS01");
        telegram.Rec.ShouldBe("UST");
    }

    [TestMethod]
    public void ToString_ValidData_TelegramFormat()
    {
        var agvAplCargoState = new AgvAplCargoState();
        agvAplCargoState.Load(TestTelegram.AgvAplCargoState);
        agvAplCargoState.ToString().ShouldBe(TestTelegram.AgvAplCargoState);
    }
}