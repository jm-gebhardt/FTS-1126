using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Telegrams;

[TestClass]
public class PickTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        const string hu = "hu";
        const string pos = "position";

        var pick = new Pick(hu, pos, DateTime.UtcNow);

        pick.Hu.ShouldBe(hu);
        pick.Pos.ShouldBe(pos);
        pick.Func.ShouldBe(nameof(Pick));
        pick.Rec.ShouldBe("EWM");
        pick.Send.ShouldBe("FTS01");
    }

    [TestMethod]
    public void Load_ValidData_LoadsPickTelegram()
    {
        var pick = new Pick("hu", "pos", DateTime.UtcNow);

        pick.Load(TestTelegram.Pick);

        pick.Hu.ShouldBe("1234567890");
        pick.Pos.ShouldBe("1040");
        pick.Func.ShouldBe("Pick");
    }

    [TestMethod]
    public void Load_DifferentTelegram_ReturnsFalse()
    {
        var pick = new Pick("hu", "pos", DateTime.UtcNow);

        pick.Load(TestTelegram.Tord).ShouldBeFalse();
    }

    [TestMethod]
    public void Load_MissingKey_ReturnsFalse()
    {
        var pick = new Pick("hu", "pos", DateTime.UtcNow);
        pick.Load("/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:Pick................/HU:1234567890........../$$:").ShouldBeFalse();
    }

    [TestMethod]
    public void ToString_ValidData_CorrectRepresentation()
    {
        var pick = new Pick("hu", "pos", DateTime.UtcNow);

        pick.Load(TestTelegram.Pick);

        pick.ToString().ShouldBeEquivalentTo(TestTelegram.Pick);
    }
}