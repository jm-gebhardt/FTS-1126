using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Processors;

[TestClass]
public class FromWePlcTelegramProcessorTest
{
    [TestMethod]
    public void GetNextTelegramToSend_NoProcessorOrCurrentTelegram_ReturnsFalse()
    {
        var fixture = new FromWePlcTelegramProcessorTestFixture()
            .Build();
        fixture.Processor.CurrentTelegram.ShouldBeNull();
        fixture.Processor.CurrentProcessor.ShouldBeNull();

        fixture.Processor.GetNextTelegramToSend(out string telegram).ShouldBeFalse();
        telegram.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetNextTelegramToSend_WithCurrentProcessor_ReturnsNextTelegramOfProcessor()
    {
        const string telegram = "The processors next telegram";
        var fixture = new FromWePlcTelegramProcessorTestFixture()
            .Build()
            .WithCurrentProcessor(telegram, true);

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(telegram);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
    }

    [TestMethod]
    public void GetNextTelegramToSend_WithoutProcessorWithCurrentTelegram_ReturnsAcknowledgementOfTelegram()
    {
        var jamState = new JamState();
        jamState.Load(TestTelegram.JamState);
        var fixture = new FromWePlcTelegramProcessorTestFixture()
            .Build()
            .WithCurrentTelegram(jamState);
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(jamState.CreateAcknowledgement());
    }

    [TestMethod]
    public void ProcessReceivedTelegram_NotConveyorTelegram_TelegramNotProcessed()
    {
        const string telegram = "Telegram that does not obey structure contract";
        var fixture = new FromWePlcTelegramProcessorTestFixture()
            .Build()
            .WithCurrentProcessor(telegram, true);

        fixture.Processor.ProcessReceivedTelegram(telegram);
        fixture.VerifyTelegramProcessed(telegram, Times.Never());
    }

    [TestMethod]
    public void ProcessReceivedTelegram_ConveyorTelegram_TelegramProcessed()
    {
        const string telegram = TestTelegram.JamState;
        var fixture = new FromWePlcTelegramProcessorTestFixture()
            .Build()
            .WithCurrentProcessor(telegram, true);

        fixture.Processor.ProcessReceivedTelegram(telegram);
        fixture.VerifyTelegramProcessed(telegram, Times.Once());
        var expectedTelegram = new ConveyorTelegram();
        expectedTelegram.Load(telegram);
        fixture.Processor.CurrentTelegram.ShouldBeEquivalentTo(expectedTelegram);
    }

    [TestMethod]
    public void ProcessReceivedTelegram_NoProcessor_TelegramAcknowledged()
    {
        const string telegram = TestTelegram.JamState;
        var fixture = new FromWePlcTelegramProcessorTestFixture()
            .Build()
            .WithoutCurrentProcessor();

        fixture.Processor.ProcessReceivedTelegram(telegram);
        fixture.Processor.CurrentTelegram.ShouldNotBeNull();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();
        fixture.VerifyTelegramProcessed(telegram, Times.Never());
    }

    [TestMethod]
    public void ProcessReceivedTelegram_ResendOfPreviousTelegram_SequenceCounterResetNotProcessedAgain()
    {
        var jamState = new JamState();
        jamState.Load(TestTelegram.JamState);
        var fixture = new FromWePlcTelegramProcessorTestFixture()
            .Build()
            .WithCurrentProcessor(jamState.CreateAcknowledgement(), true);

        // First cycle
        fixture.Processor.ProcessReceivedTelegram(jamState.ToString());
        var expectedTelegram = new ConveyorTelegram();
        expectedTelegram.Load(jamState.ToString());
        fixture.Processor.CurrentTelegram.ShouldBeEquivalentTo(expectedTelegram);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();

        fixture.Processor.GetNextTelegramToSend(out string acknowledgement).ShouldBeTrue();
        acknowledgement.ShouldBe(jamState.CreateAcknowledgement());
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();

        // Resend telegram
        fixture.Processor.ProcessReceivedTelegram(jamState.ToString());
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();

        //Only once processed in the first cycle
        fixture.VerifyTelegramProcessed(jamState.ToString(), Times.Once());
    }

    [TestMethod]
    public void Cancel_ValidData_ProcessorAndTelegramReset()
    {
        var jamState = new JamState();
        jamState.Load(TestTelegram.JamState);
        var fixture = new FromWePlcTelegramProcessorTestFixture()
            .Build()
            .WithCurrentTelegram(jamState)
            .WithCurrentProcessor(withNextTelegram: false);

        fixture.Processor.CurrentTelegram.ShouldNotBeNull();
        fixture.Processor.CurrentProcessor.ShouldNotBeNull();

        fixture.Processor.Cancel();
        fixture.Processor.CurrentTelegram.ShouldBeNull();
        fixture.Processor.CurrentProcessor.ShouldBeNull();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
    }
}
