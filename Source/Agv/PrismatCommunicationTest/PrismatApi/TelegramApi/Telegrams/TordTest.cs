using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Telegrams;

[TestClass]
public class TordTest
{
    [TestMethod]
    public void Load_ValidData_LoadsTordTelegram()
    {
        var tord = new Tord();
        tord.Load(TestTelegram.Tord).ShouldBeTrue();
        tord.Func.ShouldBe("Tord");
        tord.Dest.ShouldBe("2410");
        tord.Src.ShouldBe("1040");
        tord.OId.ShouldBe("1234567890");
        tord.OfO.ShouldBe(3);
        tord.SortId.ShouldBe(1);
    }

    [TestMethod]
    public void Load_DifferentTelegramFunction_ReturnsFalse()
    {
        var tord = new Tord();
        tord.Load(TestTelegram.Life).ShouldBeFalse();
    }

    [TestMethod]
    public void Load_AdditionalKey_ReturnsFalse()
    {
        var tord = new Tord();
        tord.Load(TestTelegram.Arrival).ShouldBeFalse();
    }
}