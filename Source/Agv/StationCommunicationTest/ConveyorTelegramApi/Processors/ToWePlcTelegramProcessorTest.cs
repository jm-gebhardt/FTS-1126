using Gebhardt.Acc.ConveyorTelegramApi.Processors;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Processors;

[TestClass]
public class ToWePlcTelegramProcessorTest
{
    [TestMethod]
    public void TrySendLifeTelegram_NoTelegramToSend_LifeTelegramQueued()
    {
        TelegramProcessorTestFixture<ToWePlcTelegramProcessor, ConveyorTelegram> fixture = new TelegramProcessorTestFixture<ToWePlcTelegramProcessor, ConveyorTelegram>(ServiceKeyProvider.AplWeStation, null)
            .Build();
        fixture.Processor.HasSendDemand().ShouldBeFalse();
        fixture.Processor.TrySendLifeTelegram();
        fixture.Processor.HasSendDemand().ShouldBeTrue();
        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();

        nextTelegram.ShouldContain("Life");
    }

    [TestMethod]
    public void ProcessReceivedTelegram_FullSendReceiveCycle_AcknowledgementHandled()
    {
        TelegramProcessorTestFixture<ToWePlcTelegramProcessor, ConveyorTelegram> fixture = new TelegramProcessorTestFixture<ToWePlcTelegramProcessor, ConveyorTelegram>(ServiceKeyProvider.AplWeStation, null)
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
        TelegramProcessorTestFixture<ToWePlcTelegramProcessor, ConveyorTelegram> fixture = new TelegramProcessorTestFixture<ToWePlcTelegramProcessor, ConveyorTelegram>(ServiceKeyProvider.AplWeStation, null)
            .Build();

        var now = DateTimeOffset.Now;
        fixture.TimeProvider.SetNow(now);

        var telegram = new Life(now.UtcDateTime);
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