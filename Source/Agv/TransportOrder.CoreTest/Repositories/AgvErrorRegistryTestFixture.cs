using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransportOrder.Persistence.Ef.DataAccess;
using TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class AgvErrorRegistryTestFixture
{
    private readonly List<Task> _tasks = new();

    public EfAgvErrorRegistry AgvErrorRegistry { get; }

    public AgvErrorRegistryTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); });
        var provider = collection.BuildServiceProvider(true);
        var contextFactory = provider.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();
        AgvErrorRegistry = new EfAgvErrorRegistry(contextFactory);
    }

    public AgvErrorRegistryTestFixture WithAgvErrorStatus(AgvErrorStatusReceived agvErrorStatus)
    {
        _tasks.Add(AgvErrorRegistry.UpdateAgvErrorStatusAsync(agvErrorStatus.Errors, agvErrorStatus.CorrelationId));
        return this;
    }

    public async Task<AgvErrorRegistryTestFixture> BuildAsync()
    {
        await Task.WhenAll(_tasks);
        return this;
    }
}