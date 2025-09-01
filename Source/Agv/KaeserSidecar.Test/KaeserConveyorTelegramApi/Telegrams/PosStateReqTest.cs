using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi.Telegrams;

[TestClass]
public class PosStateReqTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var time = DateTime.UtcNow;
        const string pos = "PS01";

        var posStateReq = new PosStateReq(pos, time);
        posStateReq.Func.ShouldBe(nameof(PosStateReq));
        posStateReq.Send.ShouldBe("FTS01");
        posStateReq.Rec.ShouldBe("UST");
        posStateReq.Time.ShouldBe(time);
        posStateReq.Pos.ShouldBe(pos);
    }

    [TestMethod]
    public void Load_ValidData_PropertiesUpdated()
    {
        var posStateReq = new PosStateReq();
        // "/Seq:001/Send:FTS01.../Rec:UST...../Time:2021:11:02:08:12:09:122/Func:PosStateReq........./Pos:PS01../$$:"
        posStateReq.Load(TestTelegram.WePosStateReq);

        posStateReq.Func.ShouldBe(nameof(PosStateReq));
        posStateReq.Send.ShouldBe("FTS01");
        posStateReq.Rec.ShouldBe("UST");
        posStateReq.Pos.ShouldBe("PS01");
    }

    [TestMethod]
    public void ToString_ValidData_TelegramFormat()
    {
        var posStateReq = new PosStateReq();
        posStateReq.Load(TestTelegram.WePosStateReq);
        posStateReq.ToString().ShouldBe(TestTelegram.WePosStateReq);
    }
}