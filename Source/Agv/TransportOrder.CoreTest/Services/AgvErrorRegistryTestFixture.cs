using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services;

public class AgvErrorRegistryTestFixture
{
    private readonly List<Task> _tasks = new();

    public EfAgvErrorRegistry AgvErrorRegistry { get; }

    public AgvErrorRegistryTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); })
            .AddTransient<IMapper>(_ => MappingHelper.GetMapper(new AgvErrorProfile()));
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