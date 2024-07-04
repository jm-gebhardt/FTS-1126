using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Processors;

[TestClass]
public class TordProcessorTest
{
    [TestMethod]
    public async Task ProcessReceivedTelegram_Tord_PublishesCreateHostOrder()
    {
        TelegramProcessorTestFixture<TordProcessor, Tord> fixture = new TelegramProcessorTestFixture<TordProcessor, Tord>()
            .Build();

        var tord = new Tord();
        tord.Load(TestTelegram.Tord);

        fixture.Processor.ProcessReceivedTelegram(TestTelegram.Tord);

        fixture.Processor.CurrentTelegram.ShouldBeEquivalentTo(tord);
        (await fixture.TestHarness.Published.Any<CreateHostOrder>()).ShouldBeTrue();
        var createHostOrder = await fixture.GetFirstPublishedMessageAsync<CreateHostOrder>();
        createHostOrder.Hu.ShouldBe(tord.Hu);
        createHostOrder.OrderDetails.ShouldBeEquivalentTo(new OrderDetails(tord.OId.ToGuid(), tord.OId, tord.Src, tord.Dest, tord.Hu, HostOrderCreator.Wms, tord.Time));
        fixture.Processor.ShouldReceiveTelegram().ShouldBeFalse();
    }

    [TestMethod]
    public async Task ProcessReceivedTelegram_NotTordTelegram_DoesNotPublishCreateHostOrderDoesNotSwapToSendingMode()
    {
        TelegramProcessorTestFixture<TordProcessor, Tord> fixture = new TelegramProcessorTestFixture<TordProcessor, Tord>()
            .Build();

        fixture.Processor.ProcessReceivedTelegram(TestTelegram.Arrival);
        fixture.Processor.CurrentTelegram.ShouldBeNull();
        (await fixture.TestHarness.Published.Any<CreateHostOrder>()).ShouldBeFalse();
        fixture.Processor.ShouldReceiveTelegram().ShouldBeTrue();
    }

    [TestMethod]
    public void GetNextTelegramToSend_NoCurrentTelegram_ReturnsFalse()
    {
        TelegramProcessorTestFixture<TordProcessor, Tord> fixture = new TelegramProcessorTestFixture<TordProcessor, Tord>()
            .Build();

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeFalse();
        nextTelegram.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetNextTelegramToSend_CurrentTelegram_ReturnsTrueAndAcknowledgement()
    {
        TelegramProcessorTestFixture<TordProcessor, Tord> fixture = new TelegramProcessorTestFixture<TordProcessor, Tord>()
            .Build();

        var tord = new Tord();
        tord.Load(TestTelegram.Tord);
        fixture.Processor.ProcessReceivedTelegram(tord.ToString());

        fixture.Processor.GetNextTelegramToSend(out string nextTelegram).ShouldBeTrue();
        nextTelegram.ShouldBeEquivalentTo(tord.CreateAcknowledgement());
    }

    [TestMethod]
    public void Cancel_CurrentTelegram_ResetsCurrentTelegram()
    {
        TelegramProcessorTestFixture<TordProcessor, Tord> fixture = new TelegramProcessorTestFixture<TordProcessor, Tord>()
            .Build();

        var tord = new Tord();
        tord.Load(TestTelegram.Tord);
        fixture.Processor.ProcessReceivedTelegram(tord.ToString());
        fixture.Processor.CurrentTelegram.ShouldNotBeNull();

        fixture.Processor.Cancel();
        fixture.Processor.CurrentTelegram.ShouldBeNull();
    }
}