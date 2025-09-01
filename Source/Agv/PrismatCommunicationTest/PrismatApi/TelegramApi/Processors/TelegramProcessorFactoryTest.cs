using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Processors;

[TestClass]
public class TelegramProcessorFactoryTest
{
    [TestMethod]
    public void GetReceiveProcessor_TordTelegram_TordProcessor()
    {
        var fixture = new TelegramProcessorFactoryTestFixture();

        var tord = new Tord();
        tord.Load(TestTelegram.Tord);

        fixture.Factory.GetReceiveProcessor(tord).ShouldBeOfType(typeof(TordProcessor));
    }

    [TestMethod]
    public void GetReceiveProcessor_LifeTelegram_LifeProcessor()
    {
        var fixture = new TelegramProcessorFactoryTestFixture();

        var life = new Life(DateTime.UtcNow);

        fixture.Factory.GetReceiveProcessor(life).ShouldBeOfType(typeof(LifeProcessor));
    }

    [TestMethod]
    public void GetReceiveProcessor_NotRegisteredTelegram_null()
    {
        var fixture = new TelegramProcessorFactoryTestFixture();
        fixture.Factory.GetReceiveProcessor(new PrismatTelegram()).ShouldBeNull();
    }
}