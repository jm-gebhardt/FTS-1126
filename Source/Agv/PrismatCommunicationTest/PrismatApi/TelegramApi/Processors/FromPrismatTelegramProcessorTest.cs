using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Processors;

[TestClass]
[Ignore]
public class FromPrismatTelegramProcessorTest
{
    [TestMethod]
    public void GetNextTelegramToSend_NoProcessorOrCurrentTelegram_ReturnsFalse()
    {
        var fixture = new FromPrismatTelegramProcessorTestFixture()
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
        var fixture = new FromPrismatTelegramProcessorTestFixture()
            .Build()
            .WithCurrentProcessor(telegram, true);

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(telegram);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
        fixture.Processor.SequenceCounter.Current.ShouldBe(2);
    }

    [TestMethod]
    public void GetNextTelegramToSend_WithoutProcessorWithCurrentTelegram_ReturnsAcknowledgementOfTelegram()
    {
        var tord = new Tord();
        tord.Load(TestTelegram.Tord);
        var fixture = new FromPrismatTelegramProcessorTestFixture()
            .Build()
            .WithCurrentTelegram(tord);
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(tord.CreateAcknowledgement());
        fixture.Processor.SequenceCounter.Current.ShouldBe(2);
    }

    [TestMethod]
    public void ProcessReceivedTelegram_NotPrismatTelegram_TelegramNotProcessed()
    {
        const string telegram = "Telegram that does not obey structure contract";
        var fixture = new FromPrismatTelegramProcessorTestFixture()
            .Build()
            .WithCurrentProcessor(telegram, true);

        fixture.Processor.ProcessReceivedTelegram(telegram);
        fixture.VerifyTelegramProcessed(telegram, Times.Never());
    }

    [TestMethod]
    public void ProcessReceivedTelegram_PrismatTelegram_TelegramProcessed()
    {
        const string telegram = TestTelegram.Tord;
        var fixture = new FromPrismatTelegramProcessorTestFixture()
            .Build()
            .WithCurrentProcessor(telegram, true);

        fixture.Processor.ProcessReceivedTelegram(telegram);
        fixture.VerifyTelegramProcessed(telegram, Times.Once());
        var expectedTelegram = new PrismatTelegram();
        expectedTelegram.Load(telegram);
        fixture.Processor.CurrentTelegram.ShouldBeEquivalentTo(expectedTelegram);
    }

    [TestMethod]
    public void ProcessReceivedTelegram_NoProcessor_NoCurrentTelegram()
    {
        const string telegram = TestTelegram.Tord;
        var fixture = new FromPrismatTelegramProcessorTestFixture()
            .Build()
            .WithoutCurrentProcessor();

        fixture.Processor.ProcessReceivedTelegram(telegram);
        fixture.Processor.CurrentTelegram.ShouldBeNull();
        fixture.VerifyTelegramProcessed(telegram, Times.Never());
    }

    [TestMethod]
    public void ProcessReceivedTelegram_ResendOfPreviousTelegram_SequenceCounterResetNotProcessedAgain()
    {
        var tord = new Tord();
        tord.Load(TestTelegram.Tord);
        var fixture = new FromPrismatTelegramProcessorTestFixture()
            .Build()
            .WithCurrentProcessor(tord.CreateAcknowledgement(), true);

        // First cycle
        fixture.Processor.ProcessReceivedTelegram(tord.ToString());
        var expectedTelegram = new PrismatTelegram();
        expectedTelegram.Load(tord.ToString());
        fixture.Processor.CurrentTelegram.ShouldBeEquivalentTo(expectedTelegram);
        fixture.Processor.SequenceCounter.Current.ShouldBe(1);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();

        fixture.Processor.GetNextTelegramToSend(out string acknowledgement).ShouldBeTrue();
        fixture.Processor.SequenceCounter.Current.ShouldBe(2);
        acknowledgement.ShouldBe(tord.CreateAcknowledgement());
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();

        // Resend telegram
        fixture.Processor.ProcessReceivedTelegram(tord.ToString());
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();
        fixture.Processor.SequenceCounter.Current.ShouldBe(1);

        //Only once processed in the first cycle
        fixture.VerifyTelegramProcessed(tord.ToString(), Times.Once());
    }

    [TestMethod]
    public void Cancel_ValidData_ProcessorAndTelegramReset()
    {
        var tord = new Tord();
        tord.Load(TestTelegram.Tord);
        var fixture = new FromPrismatTelegramProcessorTestFixture()
            .Build()
            .WithCurrentTelegram(tord)
            .WithCurrentProcessor(withNextTelegram: false);

        fixture.Processor.CurrentTelegram.ShouldNotBeNull();
        fixture.Processor.CurrentProcessor.ShouldNotBeNull();

        fixture.Processor.Cancel();
        fixture.Processor.CurrentTelegram.ShouldBeNull();
        fixture.Processor.CurrentProcessor.ShouldBeNull();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
    }
}