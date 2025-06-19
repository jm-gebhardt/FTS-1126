using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Processors;

[TestClass]
public class TordDeleteProcessorTest
{
    [TestMethod]
    public async Task ProcessReceivedTelegram_Tord_PublishesCreateHostOrder()
    {
        TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete> fixture = new TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete>(ServiceKeyProvider.FromPrismat)
            .Build();

        var tordDelete = new TordDelete();
        tordDelete.Load(TestTelegram.TordDelete);

        fixture.Processor.ProcessReceivedTelegram(TestTelegram.TordDelete);

        fixture.Processor.CurrentTelegram.ShouldBeEquivalentTo(tordDelete);
        (await fixture.TestHarness.Published.Any<CancelHostOrder>()).ShouldBeTrue();
        var cancelHostOrder = await fixture.GetFirstPublishedMessageAsync<CancelHostOrder>();
        cancelHostOrder.Hu.ShouldBe(tordDelete.Hu);
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();
    }

    [TestMethod]
    public async Task ProcessReceivedTelegram_NotTordDeleteDeleteTelegram_DoesNotPublishCreateHostOrderDoesNotSwapToSendingMode()
    {
        TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete> fixture = new TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete>(ServiceKeyProvider.FromPrismat)
            .Build();

        fixture.Processor.ProcessReceivedTelegram(TestTelegram.Arrival);
        fixture.Processor.CurrentTelegram.ShouldBeNull();
        (await fixture.TestHarness.Published.Any<CancelHostOrder>()).ShouldBeFalse();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
    }

    [TestMethod]
    public void GetNextTelegramToSend_NoCurrentTelegram_ReturnsFalse()
    {
        TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete> fixture = new TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete>(ServiceKeyProvider.FromPrismat)
            .Build();

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeFalse();
        nextTelegram.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetNextTelegramToSend_CurrentTelegram_ReturnsTrueAndAcknowledgement()
    {
        TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete> fixture = new TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete>(ServiceKeyProvider.FromPrismat)
            .Build();

        var tordDelete = new TordDelete();
        tordDelete.Load(TestTelegram.TordDelete);
        fixture.Processor.ProcessReceivedTelegram(tordDelete.ToString());

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(tordDelete.CreateAcknowledgement());
    }

    [TestMethod]
    public void Cancel_CurrentTelegram_ResetsCurrentTelegram()
    {
        TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete> fixture = new TelegramProcessorTestFixture<TordDeleteProcessor, TordDelete>(ServiceKeyProvider.FromPrismat)
            .Build();

        var tordDelete = new TordDelete();
        tordDelete.Load(TestTelegram.TordDelete);
        fixture.Processor.ProcessReceivedTelegram(tordDelete.ToString());
        fixture.Processor.CurrentTelegram.ShouldNotBeNull();

        fixture.Processor.Cancel();
        fixture.Processor.CurrentTelegram.ShouldBeNull();
    }
}