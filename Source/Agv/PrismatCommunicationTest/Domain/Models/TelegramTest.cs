using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.Telegram;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Models;

[TestClass]
public class TelegramTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var telegram = new Telegram(TestTelegram.Arrival, nameof(Arrival));
        telegram.Function.ShouldBe(nameof(Arrival));
        telegram.Id.ShouldBe(TestTelegram.Arrival.ToGuid());
        telegram.Message.ShouldBe(TestTelegram.Arrival);
    }
}