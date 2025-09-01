using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Processors;

[TestClass]
public class LifeProcessorTest
{
    [TestMethod]
    public void ProcessReceivedTelegram_LifeTelegram_CurrentTelegramSet()
    {
        TelegramProcessorTestFixture<LifeProcessor, Life> fixture = new TelegramProcessorTestFixture<LifeProcessor, Life>(ServiceKeyProvider.FromPrismat)
            .Build();

        var life = new Life(DateTime.UtcNow);
        life.Load(TestTelegram.Life);

        fixture.Processor.ProcessReceivedTelegram(TestTelegram.Life);

        fixture.Processor.CurrentTelegram.ShouldBeEquivalentTo(life);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();
    }

    [TestMethod]
    public void GetNextTelegramToSend_CurrentTelegram_ReturnsAcknowledgement()
    {
        TelegramProcessorTestFixture<LifeProcessor, Life> fixture = new TelegramProcessorTestFixture<LifeProcessor, Life>(ServiceKeyProvider.FromPrismat)
            .Build();

        var life = new Life(DateTime.UtcNow);
        life.Load(TestTelegram.Life);

        fixture.Processor.ProcessReceivedTelegram(TestTelegram.Life);

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(life.CreateAcknowledgement());
    }

    [TestMethod]
    public void GetNextTelegramToSend_NoCurrentTelegram_ReturnsFalse()
    {
        TelegramProcessorTestFixture<LifeProcessor, Life> fixture = new TelegramProcessorTestFixture<LifeProcessor, Life>(ServiceKeyProvider.FromPrismat)
            .Build();

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeFalse();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
        nextTelegram.ShouldBeEmpty();
    }
}