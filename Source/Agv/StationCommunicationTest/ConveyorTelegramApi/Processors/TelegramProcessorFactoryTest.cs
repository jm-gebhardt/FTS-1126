using Gebhardt.Acc.ConveyorTelegramApi.Processors;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Processors;

[TestClass]
public class TelegramProcessorFactoryTest
{
    [TestMethod]
    public void GetReceiveProcessor_LifeTelegram_LifeProcessor()
    {
        var fixture = new TelegramProcessorFactoryTestFixture();

        var life = new Life(DateTime.UtcNow);

        fixture.Factory.GetReceiveProcessor(life).ShouldBeOfType(typeof(LifeProcessor));
    }

    [TestMethod]
    public void GetReceiveProcessor_JamStateTelegram_JamStateProcessor()
    {
        var fixture = new TelegramProcessorFactoryTestFixture();

        var jamState = new JamState();

        fixture.Factory.GetReceiveProcessor(jamState).ShouldBeOfType(typeof(JamStateProcessor));
    }

    [TestMethod]
    public void GetReceiveProcessor_NotRegisteredTelegram_Null()
    {
        var fixture = new TelegramProcessorFactoryTestFixture();

        fixture.Factory.GetReceiveProcessor(new ConveyorTelegram()).ShouldBeNull();
    }
}