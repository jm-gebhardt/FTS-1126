using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Telegrams;

[TestClass]
public class StatusTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var time = DateTime.UtcNow;
        const SystemState state = SystemState.AUS;
        const int err = 4242;
        const string errorInfo = "There is an error";

        var status = new Status(state, time, err, errorInfo);
        status.Func.ShouldBe(nameof(Status));
        status.Rec.ShouldBe("EWM");
        status.Send.ShouldBe("FTS01");
        status.Time.ShouldBe(time);
        status.Pos.ShouldBe("FTS001");
        status.Err.ShouldBe(err);
        status.Info.ShouldBe(errorInfo);
        status.State.ShouldBe(state);
    }

    [TestMethod]
    public void Load_ValidData_PropertiesUpdated()
    {
        var status = new Status();
        // "/Seq:001/Send:FTS01.../Rec:EWM...../Time:2021:11:02:08:12:09:122/Func:Status............../Pos:FTS001..../State:AUTO/ERR:0000/Info:................................................../$$:"
        status.Load(TestTelegram.Status);

        status.Func.ShouldBe(nameof(Status));
        status.Rec.ShouldBe("EWM");
        status.Send.ShouldBe("FTS01");
        status.Pos.ShouldBe("FTS001");
        status.Err.ShouldBe(0);
        status.Info.ShouldBe("");
        status.State.ShouldBe(SystemState.AUTO);
    }

    [TestMethod]
    public void ToString_ValidData_TelegramFormat()
    {
        var status = new Status();
        status.Load(TestTelegram.Status);
        status.ToString().ShouldBe(TestTelegram.Status);
    }
}