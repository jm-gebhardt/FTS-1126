using Gebhardt.Acc.PrismatCommunication.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Processors;

[TestClass]
public class ToPrismatTelegramProcessorTest
{
    [TestMethod]
    public void TrySendLifeTelegram_NoTelegramToSend_LifeTelegramQueued()
    {
        TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram> fixture = new TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram>(ServiceKeyProvider.ToPrismat, true)
            .Build();
        fixture.Processor.HasSendDemand().ShouldBeFalse();
        fixture.Processor.TrySendLifeTelegram();
        fixture.Processor.HasSendDemand().ShouldBeTrue();
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        nextTelegram.ShouldContain("Life");
    }

    [TestMethod]
    public void GetNextTelegramToSend_NoDemandLifeTelegramDue_ReturnsLifeTelegram()
    {
        TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram> fixture = new TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram>(ServiceKeyProvider.ToPrismat, true)
            .Build();
        fixture.Processor.HasSendDemand().ShouldBeFalse();
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        fixture.Processor.CurrentTelegram.ShouldNotBeNull();
        fixture.Processor.CurrentTelegram.Func.ShouldBe("Life");

        nextTelegram.ShouldBe(fixture.Processor.CurrentTelegram.ToString());
    }

    [TestMethod]
    public void GetNextTelegramToSend_HasSendDemand_ReturnsTelegramFromQueue()
    {
        TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram> fixture = new TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram>(ServiceKeyProvider.ToPrismat, true)
            .Build();
        var now = DateTime.UtcNow;
        var telegram = new Arrival(DateTime.UtcNow, "position", "hu");
        fixture.TimeProvider.SetNow(now);
        fixture.Processor.EnqueueTelegram(telegram).ShouldBeTrue();
        fixture.Processor.HasSendDemand().ShouldBeTrue();
        (now - fixture.Processor.LastTelegramSend).ShouldBeGreaterThan(TimeSpan.FromMinutes(1));

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBe(telegram.ToString());
        fixture.Processor.CurrentTelegram.ShouldBe(telegram);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
        (now - fixture.Processor.LastTelegramSend).ShouldBeLessThan(TimeSpan.FromMinutes(1));
    }

    [TestMethod]
    public void ProcessReceivedTelegram_FullSendReceiveCycle_AcknowledgementHandled()
    {
        TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram> fixture = new TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram>(ServiceKeyProvider.ToPrismat, true)
            .Build();
        var telegram = new Arrival(DateTime.UtcNow, "position", "hu");
        fixture.Processor.EnqueueTelegram(telegram).ShouldBeTrue();
        fixture.Processor.GetNextTelegramToSend(out string _).ShouldBeTrue();
        fixture.Processor.SequenceCounter.Current.ShouldBe(1);

        fixture.Processor.ProcessReceivedTelegram(telegram.CreateAcknowledgement());
        fixture.Processor.SequenceCounter.Current.ShouldBe(2);
        fixture.Processor.CurrentTelegram.ShouldBeNull();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();
    }

    [TestMethod]
    public void GetNextTelegramToSend_NoAcknowledgementReceived_ResendUntilMaxRetryIsReached()
    {
        TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram> fixture = new TelegramProcessorTestFixture<ToPrismatTelegramProcessor, PrismatTelegram>(ServiceKeyProvider.ToPrismat, true)
            .Build();

        var now = DateTime.UtcNow;
        fixture.TimeProvider.SetNow(now);

        var telegram = new Arrival(DateTime.UtcNow, "position", "hu");
        fixture.Processor.EnqueueTelegram(telegram).ShouldBeTrue();

        // First sending attempt
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(telegram.ToString());
        fixture.Processor.LastTelegramSend.ShouldBe(now);
        fixture.Processor.AwaitAcknowledgementStart.ShouldBe(now);
        fixture.Processor.SequenceCounter.Current.ShouldBe(1);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
        fixture.Processor.RemainingAttempts.ShouldBe(fixture.Processor.MaxRetryAttempts);

        while (fixture.Processor.RemainingAttempts > 0)
        {
            // Acknowledgement waiting time passed
            now = fixture.Processor.AwaitAcknowledgementStart.AddMilliseconds(fixture.Processor.MaxAcknowledgementWaitTime);
            fixture.TimeProvider.SetNow(now);
            fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();

            // Retry sending
            fixture.Processor.GetNextTelegramToSend(out nextTelegram).ShouldBeTrue();
            nextTelegram.ShouldBeEquivalentTo(telegram.ToString());
            fixture.Processor.LastTelegramSend.ShouldBe(now);
            fixture.Processor.AwaitAcknowledgementStart.ShouldBe(now);
            fixture.Processor.SequenceCounter.Current.ShouldBe(1);
            fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
        }

        //Ready for sending
        fixture.Processor.HasSendDemand().ShouldBeFalse();
        fixture.Processor.GetNextTelegramToSend(out nextTelegram).ShouldBeFalse();
        nextTelegram.ShouldBeEmpty();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();
    }
}