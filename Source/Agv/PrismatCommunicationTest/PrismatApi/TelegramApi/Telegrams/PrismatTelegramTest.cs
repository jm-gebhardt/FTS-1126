using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Shouldly;
using System.Globalization;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Telegrams;

[TestClass]
public class PrismatTelegramTest
{
    [TestMethod]
    public void Construct_ValidData_EndSymbolSet()
    {
        var telegram = new PrismatTelegram();
        telegram.EndSymbol.ShouldBe("$$:");
    }

    [TestMethod]
    public void Load_ValidData_LoadsTelegramHeader()
    {
        var telegram = new PrismatTelegram();
        telegram.Load(TestTelegram.Tord).ShouldBeTrue();
        telegram.Func.ShouldBe("Tord");
        telegram.Rec.ShouldBe("FTS01");
        telegram.Send.ShouldBe("EWM");
        telegram.Time.ShouldBe(DateTime.ParseExact("2021:11:02:08:12:09:122", "yyyy:MM:dd:HH:mm:ss:fff", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Load_UnexpectedFormat_ReturnsFalse()
    {
        var telegram = new PrismatTelegram();
        telegram.Load("/Seq:UNEXPECTED STRING/Send:EWM...../Rec:FTS01.../Time:2020:12:13:20:12:09:122/Func:Life……………./$$:").ShouldBeFalse();
    }

    [TestMethod]
    public void Load_MissingKey_ReturnsFalse()
    {
        var telegram = new PrismatTelegram();
        telegram.Load("/Func:Life……………./$$:").ShouldBeFalse();
    }

    [TestMethod]
    public void CreateAcknowledgement_ValidData_ReturnsAcknowledgement()
    {
        var telegram = new PrismatTelegram();
        telegram.Load("/Seq:001/Send:EWM...../Rec:FTS01.../Time:2020:12:13:20:12:09:122/Func:Life................/$$:").ShouldBeTrue();
        telegram.CreateAcknowledgement().ShouldBe("/Seq:001/Send:FTS01.../Rec:EWM...../Time:2020:12:13:20:12:09:122/Func:Life................/$$:");
    }
}