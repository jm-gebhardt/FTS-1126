using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

[TestClass]
public class AgvWeStationTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        const string station = "WE01";
        const string pick = "1040";
        const string drop = "1060";
        var time = DateTime.UtcNow;

        var telegram = new AgvWeStation(station, drop, pick, time);

        telegram.Func.ShouldBe(nameof(AgvWeStation));
        telegram.Drop.ShouldBe(drop);
        telegram.Pick.ShouldBe(pick);
        telegram.Station.ShouldBe(station);
        telegram.Time.ShouldBe(time);
        telegram.Send.ShouldBe("FTS01");
        telegram.Rec.ShouldBe("UST");
    }

    [TestMethod]
    public void ToString_ValidData_TelegramFormat()
    {
        var agvWeStation = new AgvWeStation("any", "any", "any", DateTime.UtcNow);
        agvWeStation.Load(TestTelegram.AgvWeStation);
        agvWeStation.ToString().ShouldBe(TestTelegram.AgvWeStation);
    }
}