using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.Telegram;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Domain.Services;

[TestClass]
public class TelegramQueuePersistenceServiceTest
{
    [TestMethod]
    public async Task OnItemEnqueued_ValidData_TelegramStored()
    {
        var fixture = new TelegramQueuePersistenceServiceTestFixture();

        var arrival = new Arrival(DateTime.UtcNow, "pos", "hu");
        arrival.Load(TestTelegram.Arrival);

        fixture.Queue.Enqueue(arrival);

        ICollection<Telegram> storedTelegrams = await fixture.Repository.GetAll();
        storedTelegrams.Count.ShouldBe(1);

        var telegram = storedTelegrams.Single();
        telegram.Function.ShouldBe(arrival.Func);
        telegram.Message.ShouldBe(TestTelegram.Arrival);
        telegram.Id.ShouldBe(TestTelegram.Arrival.ToGuid());
    }

    [TestMethod]
    public async Task OnItemDequeued_TelegramListedForRemoval_RemovedOnNextEnqueue()
    {
        var fixture = new TelegramQueuePersistenceServiceTestFixture();

        var arrival = new Arrival(DateTime.UtcNow, "pos", "hu");
        arrival.Load(TestTelegram.Arrival);

        fixture.Queue.Enqueue(arrival);
        (await fixture.Repository.GetAll()).Count.ShouldBe(1);

        fixture.Queue.TryDequeue(out var dequeuedTelegram);

        dequeuedTelegram.Func.ShouldBe(arrival.Func);
        (await fixture.Repository.GetAll()).Count.ShouldBe(1);

        fixture.Queue.Enqueue(new Life(DateTime.UtcNow));
        (await fixture.Repository.GetAll()).Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task ReloadQueue_ValidData_TelegramsEnqueued()
    {
        var fixture = new TelegramQueuePersistenceServiceTestFixture();
        var telegram = new Telegram(TestTelegram.Arrival, nameof(Arrival));
        await fixture.Repository.AddAsync(telegram);
        (await fixture.Repository.GetAll()).Count.ShouldBe(1);
        fixture.Queue.Count.ShouldBe(0);

        await fixture.Service.ReloadQueue();

        fixture.Queue.Count.ShouldBe(1);
        (await fixture.Repository.GetAll()).Count.ShouldBe(1);
    }
}