using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

public class TelegramQueuePersistenceServiceTestFixture<TTelegramType>: FixtureBase
where TTelegramType: Telegram
{
    public readonly ObservableConcurrentQueue<ConveyorTelegram> Queue;
    public readonly TelegramQueuePersistenceService Service;
    public readonly ITelegramRepository Repository;
    private readonly EfTelegramRepositoryTestFixture<TTelegramType> _repositoryTestFixture = new();
    
    public TelegramQueuePersistenceServiceTestFixture()
    {
        AddServices(s =>
        {
            s.AddSingleton<ObservableConcurrentQueue<ConveyorTelegram>>();
            s.AddSingleton<TelegramQueuePersistenceService>();
            s.AddSingleton<ITelegramRepository>(_repositoryTestFixture.Repository);
            s.AddTransient(_ => new Mock<ILogger<TelegramQueuePersistenceService>>().Object);
        });
        Build();

        Repository = _repositoryTestFixture.Repository;
        Queue = Provider.GetRequiredService<ObservableConcurrentQueue<ConveyorTelegram>>();
        Service = Provider.GetRequiredService<TelegramQueuePersistenceService>();
        Queue.ItemDequeued += Service.OnItemDequeued;
        Queue.ItemEnqueued += Service.OnItemEnqueued;
    }
}