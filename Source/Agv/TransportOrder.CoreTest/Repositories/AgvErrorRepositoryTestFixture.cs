using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class AgvErrorRepositoryTestFixture
{
    private readonly List<Task> _tasks = new();

    public EfAgvErrorRepository AgvErrorRepository { get; }

    public AgvErrorRepositoryTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); });
        var provider = collection.BuildServiceProvider(true);
        var contextFactory = provider.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();
        AgvErrorRepository = new EfAgvErrorRepository(contextFactory);
    }

    public AgvErrorRepositoryTestFixture WithAgvErrorStatus(AgvErrorStatusReceived agvErrorStatus)
    {
        _tasks.Add(AgvErrorRepository.AddOrUpdateAgvErrorStatusAsync(agvErrorStatus.Errors, agvErrorStatus.CorrelationId));
        return this;
    }

    public async Task<AgvErrorRepositoryTestFixture> BuildAsync()
    {
        await Task.WhenAll(_tasks);

        return this;
    }
}