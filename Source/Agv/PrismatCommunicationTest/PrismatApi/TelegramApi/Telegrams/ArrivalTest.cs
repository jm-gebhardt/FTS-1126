using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Telegrams;

[TestClass]
public class ArrivalTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var time = DateTime.UtcNow;
        const string hu = "handling unit";
        const string pos = "1040";
       
        var arrival = new Arrival(time, pos, hu);
        
        arrival.Hu.ShouldBe(hu);
        arrival.Pos.ShouldBe(pos);
        arrival.Time.ShouldBe(time);
        arrival.Func.ShouldBe(nameof(Arrival));
        arrival.Rec.ShouldBe("EWM");
        arrival.Send.ShouldBe("FTS01");
    }

    [TestMethod]
    public void Load_ValidData_PropertiesUpdated()
    {
        var arrival = new Arrival(DateTime.UtcNow, "pos", "hu");
        arrival.Load(TestTelegram.Arrival);
        arrival.Hu.ShouldBe("1234567890");
        arrival.Pos.ShouldBe("1050");
    }

    [TestMethod]
    public void ToString_ValidData_TelegramFormat()
    {
        var arrival = new Arrival(DateTime.UtcNow, "pos", "hu");
        arrival.Load(TestTelegram.Arrival);
        arrival.ToString().ShouldBe(TestTelegram.Arrival);
    }
}