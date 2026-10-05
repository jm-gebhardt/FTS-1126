using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.ConveyorTelegramApi.Processors;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Processors;

[TestClass]
public class JamStateProcessorTest
{
    [TestMethod]
    public async Task ProcessReceivedTelegram_JamState_PublishesJamStateReceived()
    {
        var fixture = new TelegramProcessorTestFixture<JamStateProcessor, JamState>(ServiceKeyProvider.FromWeStation, null)
            .Build();
        var telegram = new JamState();
        telegram.Load(TestTelegram.JamState);

        fixture.Processor.ProcessReceivedTelegram(TestTelegram.JamState);

        fixture.Processor.CurrentTelegram.ShouldBeEquivalentTo(telegram);
        (await fixture.TestHarness.Published.Any<JamStateReceived>()).ShouldBeTrue();
        var jamStateReceived = await fixture.GetFirstPublishedMessageAsync<JamStateReceived>();
        jamStateReceived.Station.ShouldBe(telegram.Pos);
        jamStateReceived.Status.ShouldBe(telegram.State);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();
    }

    [TestMethod]
    public async Task ProcessReceivedTelegram_NotJamState_DoesNotPublishJamStateReceived()
    {
        var fixture = new TelegramProcessorTestFixture<JamStateProcessor, JamState>(ServiceKeyProvider.FromWeStation, null)
            .Build();
        var telegram = new Life(DateTime.UtcNow);
        telegram.Load(TestTelegram.Life);

        fixture.Processor.ProcessReceivedTelegram(TestTelegram.Life);

        fixture.Processor.CurrentTelegram.ShouldBeNull();
        (await fixture.TestHarness.Published.Any<JamStateReceived>()).ShouldBeFalse();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
    }

    [TestMethod]
    public void GetNextTelegramToSend_NoCurrentTelegram_ReturnsFalse()
    {
        var fixture = new TelegramProcessorTestFixture<JamStateProcessor, JamState>(ServiceKeyProvider.FromWeStation, true)
            .Build();

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeFalse();
        nextTelegram.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetNextTelegramToSend_CurrentTelegram_ReturnsTrueAndAcknowledgement()
    {
        var fixture = new TelegramProcessorTestFixture<JamStateProcessor, JamState>(ServiceKeyProvider.FromWeStation, true)
            .Build();

        var telegram = new JamState();
        telegram.Load(TestTelegram.JamState);
        fixture.Processor.ProcessReceivedTelegram(TestTelegram.JamState);

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(telegram.CreateAcknowledgement());
    }

    [TestMethod]
    public void Cancel_CurrentTelegram_ResetsCurrentTelegram()
    {
        var fixture = new TelegramProcessorTestFixture<JamStateProcessor, JamState>(ServiceKeyProvider.FromWeStation, true)
            .Build();

        var telegram = new JamState();
        telegram.Load(TestTelegram.JamState);
        fixture.Processor.ProcessReceivedTelegram(TestTelegram.JamState);
        fixture.Processor.CurrentTelegram.ShouldNotBeNull();

        fixture.Processor.Cancel();
        fixture.Processor.CurrentTelegram.ShouldBeNull();
    }
}