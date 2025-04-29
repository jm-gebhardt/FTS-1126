using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

[TestClass]
public class TordTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        const string hu = "12345678";
        const string dest = "1060";
        const string src = "PS01";
        var time = DateTime.UtcNow;

        var telegram = new Tord(src, dest, hu, time);

        telegram.Func.ShouldBe(nameof(Tord));
        telegram.Src.ShouldBe(src);
        telegram.Dest.ShouldBe(dest);
        telegram.Hu.ShouldBe(hu);
        telegram.Time.ShouldBe(time);
        telegram.Send.ShouldBe("FTS01");
        telegram.Rec.ShouldBe("UST");
    }

    [TestMethod]
    public void ToString_ValidData_TelegramFormat()
    {
        var agvWeStation = new Tord();
        agvWeStation.Load(TestTelegram.TordPlc);
        agvWeStation.ToString().ShouldBe(TestTelegram.TordPlc);
    }
}