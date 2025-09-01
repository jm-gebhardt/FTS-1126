using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Telegrams;

[TestClass]
public class TordErrorTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var time = DateTime.UtcNow;
        const string hu = "handling unit";
        const string dest = "9060";
        const string source = "1040";
        const string info = "error information";
        const int err = 9010;

        var tordError = new TordError(hu, source, dest, info, err, time);
        tordError.Func.ShouldBe(nameof(TordError));
        tordError.Rec.ShouldBe("EWM");
        tordError.Send.ShouldBe("FTS01");
        tordError.Dest.ShouldBe(dest);
        tordError.Err.ShouldBe(err);
        tordError.Info.ShouldBe(info);
        tordError.Src.ShouldBe(source);
        tordError.Time.ShouldBe(time);
        tordError.Hu.ShouldBe(hu);
    }

    [TestMethod]
    public void Load_ValidData_PropertiesUpdated()
    {
        var tordError = new TordError();
        //  "/Seq:001/Send:FTS01.../Rec:EWM..../Time:2021:11:02:08:12:09:122/Func:TordError.........../HU:5010000010........../Err:9010/Src:1020....../Dest:9999....../Info:Ziel-Station nicht bekannt......................../$$:";
        tordError.Load(TestTelegram.TordError);

        tordError.Func.ShouldBe(nameof(TordError));
        tordError.Rec.ShouldBe("EWM");
        tordError.Send.ShouldBe("FTS01");
        tordError.Dest.ShouldBe("9999");
        tordError.Err.ShouldBe(9010);
        tordError.Info.ShouldBe("Ziel-Station nicht bekannt");
        tordError.Src.ShouldBe("1020");
        tordError.Hu.ShouldBe("5010000010");
    }

    [TestMethod]
    public void ToString_ValidData_TelegramFormat()
    {
        var tordError = new TordError();
        tordError.Load(TestTelegram.TordError);
        tordError.ToString().ShouldBe(TestTelegram.TordError);
    }
}