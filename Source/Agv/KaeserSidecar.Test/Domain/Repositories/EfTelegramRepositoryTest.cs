using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

[TestClass]
public class EfTelegramRepositoryTest
{
    [TestMethod]
    public async Task Add_AgvWeStationTelegram_TelegramAdded()
    {
        var fixture = new EfTelegramRepositoryTestFixture<AgvWeStationTelegram>();

        var telegram = new AgvWeStationTelegram(TestTelegram.AgvWeStation, nameof(AgvWeStation));

        await fixture.Repository.AddAsync(telegram);

        (await fixture.Repository.GetByIdAsync(telegram.Id)).Message.ShouldBe(TestTelegram.AgvWeStation);
    }

    [TestMethod]
    public async Task Add_AgvAplCargoStateTelegram_TelegramAdded()
    {
        var fixture = new EfTelegramRepositoryTestFixture<AgvAplCargoStateTelegram>();

        var telegram = new AgvAplCargoStateTelegram(TestTelegram.AgvAplCargoState, nameof(AgvAplCargoState));

        await fixture.Repository.AddAsync(telegram);

        (await fixture.Repository.GetByIdAsync(telegram.Id)).Message.ShouldBe(TestTelegram.AgvAplCargoState);
    }

    [TestMethod]
    public async Task GetAll_AgvWeStationTelegram_TelegramsReturned()
    {
        var fixture = new EfTelegramRepositoryTestFixture<AgvWeStationTelegram>();

        var telegram = new AgvWeStationTelegram(TestTelegram.AgvWeStation, nameof(AgvWeStation));

        await fixture.Repository.AddAsync(telegram);

        ICollection<Telegram> telegrams = await fixture.Repository.GetAll();

        telegrams.Count.ShouldBe(1);
        telegrams.Single().Message.ShouldBe(TestTelegram.AgvWeStation);
    }

    [TestMethod]
    public async Task GetAll_AgvAplCargoStateTelegram_TelegramsReturned()
    {
        var fixture = new EfTelegramRepositoryTestFixture<AgvAplCargoStateTelegram>();

        var telegram = new AgvAplCargoStateTelegram(TestTelegram.AgvAplCargoState, nameof(AgvAplCargoState));

        await fixture.Repository.AddAsync(telegram);

        ICollection<Telegram> telegrams = await fixture.Repository.GetAll();

        telegrams.Count.ShouldBe(1);
        telegrams.Single().Message.ShouldBe(TestTelegram.AgvAplCargoState);
    }

    [TestMethod]
    public async Task Remove_AgvWeStationTelegram_TelegramAdded()
    {
        var fixture = new EfTelegramRepositoryTestFixture<AgvWeStationTelegram>();

        var telegram = new AgvWeStationTelegram(TestTelegram.AgvWeStation, nameof(AgvWeStation));

        await fixture.Repository.AddAsync(telegram);
        await fixture.Repository.RemoveAsync(telegram);

        (await fixture.Repository.GetAll()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Remove_AgvAplCargoStateTelegram_TelegramAdded()
    {
        var fixture = new EfTelegramRepositoryTestFixture<AgvAplCargoStateTelegram>();

        var telegram = new AgvAplCargoStateTelegram(TestTelegram.AgvAplCargoState, nameof(AgvAplCargoState));

        await fixture.Repository.AddAsync(telegram);
        await fixture.Repository.RemoveAsync(telegram);

        (await fixture.Repository.GetAll()).ShouldBeEmpty();
    }
}