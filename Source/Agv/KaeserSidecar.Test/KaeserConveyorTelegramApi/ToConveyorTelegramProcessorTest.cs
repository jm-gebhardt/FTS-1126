using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Life = Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams.Life;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

[TestClass]
public class ToConveyorTelegramProcessorTest
{
    [TestMethod]
    public void TrySendLifeTelegram_NoTelegramToSend_LifeTelegramQueued()
    {
        TelegramProcessorTestFixture<ToConveyorTelegramProcessor, ConveyorTelegram> fixture = new TelegramProcessorTestFixture<ToConveyorTelegramProcessor, ConveyorTelegram>(null)
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
        var fixture = new ToConveyorTelegramProcessorTestFixture();
        fixture.Processor.HasSendDemand().ShouldBeFalse();
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        fixture.Processor.CurrentTelegram.ShouldNotBeNull();
        fixture.Processor.CurrentTelegram.Func.ShouldBe("Life");

        nextTelegram.ShouldBe(fixture.Processor.CurrentTelegram.ToString());
        ToConveyorTelegramProcessorTestFixture.DequeueEventTriggered.ShouldBeTrue();
    }

    [TestMethod]
    public void ProcessReceivedTelegram_FullSendReceiveCycle_AcknowledgementHandled()
    {
        TelegramProcessorTestFixture<ToConveyorTelegramProcessor, ConveyorTelegram> fixture = new TelegramProcessorTestFixture<ToConveyorTelegramProcessor, ConveyorTelegram>(null)
            .Build();
        var telegram = new Life(DateTime.UtcNow);
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
        TelegramProcessorTestFixture<ToConveyorTelegramProcessor, ConveyorTelegram> fixture = new TelegramProcessorTestFixture<ToConveyorTelegramProcessor, ConveyorTelegram>(null)
            .Build();

        var now = DateTime.UtcNow;
        fixture.TimeProvider.SetNow(now);

        var telegram = new Life(DateTime.UtcNow);
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