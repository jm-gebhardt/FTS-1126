using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

[TestClass]
public class TelegramQueuePersistenceServiceTest
{
    [TestMethod]
    public async Task OnItemEnqueued_AgvWeStationTelegram_TelegramStored()
    {
        var fixture = new TelegramQueuePersistenceServiceTestFixture<AgvWeStationTelegram>();

        var agvWeStation = new AgvWeStation();
        agvWeStation.Load(TestTelegram.AgvWeStation);

        fixture.Queue.Enqueue(agvWeStation);

        ICollection<Telegram> storedTelegrams = await fixture.Repository.GetAll();
        storedTelegrams.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task OnItemEnqueued_AgvAplCargoStateTelegram_TelegramStored()
    {
        var fixture = new TelegramQueuePersistenceServiceTestFixture<AgvAplCargoStateTelegram>();

        var agvAplCargoState = new AgvAplCargoState();
        agvAplCargoState.Load(TestTelegram.AgvAplCargoState);

        fixture.Queue.Enqueue(agvAplCargoState);

        ICollection<Telegram> storedTelegrams = await fixture.Repository.GetAll();
        storedTelegrams.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task OnItemDequeued_TelegramListedForRemoval_RemovedOnNextEnqueue()
    {
        var fixture = new TelegramQueuePersistenceServiceTestFixture<AgvAplCargoStateTelegram>();

        var agvAplCargoState = new AgvAplCargoState();
        agvAplCargoState.Load(TestTelegram.AgvAplCargoState);

        fixture.Queue.Enqueue(agvAplCargoState);
        (await fixture.Repository.GetAll()).Count.ShouldBe(1);

        fixture.Queue.TryDequeue(out var dequeuedTelegram);

        dequeuedTelegram.Func.ShouldBe(agvAplCargoState.Func);
        (await fixture.Repository.GetAll()).Count.ShouldBe(1);

        fixture.Queue.Enqueue(new Life(DateTime.UtcNow));
        (await fixture.Repository.GetAll()).Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task ReloadQueue_ValidData_TelegramsEnqueued()
    {
        var fixture = new TelegramQueuePersistenceServiceTestFixture<AgvAplCargoStateTelegram>();
        var telegram =  Telegram.Create(TestTelegram.AgvAplCargoState, nameof(AgvAplCargoState));
        await fixture.Repository.AddAsync(telegram);
        (await fixture.Repository.GetAll()).Count.ShouldBe(1);
        fixture.Queue.Count.ShouldBe(0);

        await fixture.Service.ReloadQueue();

        fixture.Queue.Count.ShouldBe(1);
        (await fixture.Repository.GetAll()).Count.ShouldBe(1);
    }
}