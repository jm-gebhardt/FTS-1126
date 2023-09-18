using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class ActionGroupRegistryTestFixture
{
    private readonly List<Task> _actionGroupTasks = new();

    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    public EfActionGroupRegistry ActionGroupRegistry { get; }

    public ActionGroupRegistryTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); });
        var provider = collection.BuildServiceProvider(true);
        ContextFactory = provider.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();
        ActionGroupRegistry = new EfActionGroupRegistry(ContextFactory);
    }

    public ActionGroupRegistryTestFixture WithActionGroups(IEnumerable<ActionGroup> actionGroups)
    {
        foreach (var actionGroup in actionGroups)
        {
            WithActionGroup(actionGroup);
        }
        return this;
    }

    public ActionGroupRegistryTestFixture WithActionGroup(ActionGroup actionGroup)
    {
        _actionGroupTasks.Add(ActionGroupRegistry.AddAsync(actionGroup));
        return this;
    }

    public async Task<ActionGroupRegistryTestFixture> BuildAsync()
    {
        await Task.WhenAll(_actionGroupTasks);
        return this;
    }
}