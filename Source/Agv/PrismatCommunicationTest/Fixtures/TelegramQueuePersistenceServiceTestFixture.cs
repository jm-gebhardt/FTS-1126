using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class TelegramQueuePersistenceServiceTestFixture: FixtureBase
{
    public readonly ObservableConcurrentQueue<PrismatTelegram> Queue;
    public readonly TelegramQueuePersistenceService Service;
    public readonly ITelegramRepository Repository;
    private readonly TelegramRepositoryTestFixture _repositoryTestFixture = new();

    public TelegramQueuePersistenceServiceTestFixture()
    {
        AddServices(s =>
        {
            s.AddSingleton<ObservableConcurrentQueue<PrismatTelegram>>();
            s.AddSingleton<TelegramQueuePersistenceService>();
            s.AddSingleton(_repositoryTestFixture.Repository);
            s.AddTransient(_ => new Mock<ILogger<TelegramQueuePersistenceService>>().Object);
        });
        Build();
        
        Repository = _repositoryTestFixture.Repository;
        Queue = Provider.GetRequiredService<ObservableConcurrentQueue<PrismatTelegram>>();
        Service = Provider.GetRequiredService<TelegramQueuePersistenceService>();
        Queue.ItemDequeued += Service.OnItemDequeued;
        Queue.ItemEnqueued += Service.OnItemEnqueued;
    }
}