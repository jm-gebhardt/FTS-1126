using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Telegrams;

[TestClass]
public class JamStateTest
{
    [TestMethod]
    public void Load_ValidData_PropertiesSet()
    {
        var jamState = new JamState();
        //"/Seq:001/Send:UST...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:JamState............/Pos:PS01../State:1/$$:";
        jamState.Load(TestTelegram.JamState);

        jamState.Pos.ShouldBe("PS01");
        jamState.State.ShouldBe(1);
    }
}