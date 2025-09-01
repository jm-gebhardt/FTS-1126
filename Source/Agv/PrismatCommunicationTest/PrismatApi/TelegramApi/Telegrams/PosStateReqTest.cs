using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Telegrams;

[TestClass]
public class PosStateReqTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var time = DateTime.UtcNow;
        const string pos = "FTS001";

        var posStateReq = new PosStateReq(pos, time);
        posStateReq.Func.ShouldBe(nameof(PosStateReq));
        posStateReq.Send.ShouldBe("EWM");
        posStateReq.Rec.ShouldBe("FTS01");
        posStateReq.Time.ShouldBe(time);
        posStateReq.Pos.ShouldBe(pos);
    }

    [TestMethod]
    public void Load_ValidData_PropertiesUpdated()
    {
        var posStateReq = new PosStateReq();
        // "/Seq:001/Send:EWM...../Rec:FTS01.../Time:2021:11:02:08:12:09:122/Func:PosStateReq........./Pos:FTS001/$$:"
        posStateReq.Load(TestTelegram.PosStateReq);

        posStateReq.Func.ShouldBe(nameof(PosStateReq));
        posStateReq.Send.ShouldBe("EWM");
        posStateReq.Rec.ShouldBe("FTS01");
        posStateReq.Pos.ShouldBe("FTS001");
    }

    [TestMethod]
    public void ToString_ValidData_TelegramFormat()
    {
        var posStateReq = new PosStateReq();
        posStateReq.Load(TestTelegram.PosStateReq);
        posStateReq.ToString().ShouldBe(TestTelegram.PosStateReq);
    }
}