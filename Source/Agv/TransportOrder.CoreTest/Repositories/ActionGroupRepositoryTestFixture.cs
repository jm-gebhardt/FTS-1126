using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class ActionGroupRepositoryTestFixture
{
    private readonly List<Task> _actionGroupTasks = new();

    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    public EfActionGroupRepository ActionGroupRepository { get; }

    public ActionGroupRepositoryTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); });
        var provider = collection.BuildServiceProvider(true);
        ContextFactory = provider.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();
        ActionGroupRepository = new EfActionGroupRepository(ContextFactory);
    }

    public ActionGroupRepositoryTestFixture WithActionGroups(IEnumerable<ActionGroup> actionGroups)
    {
        foreach (var actionGroup in actionGroups)
        {
            WithActionGroup(actionGroup);
        }
        return this;
    }

    public ActionGroupRepositoryTestFixture WithActionGroup(ActionGroup actionGroup)
    {
        _actionGroupTasks.Add(ActionGroupRepository.AddAsync(actionGroup));
        return this;
    }

    public async Task<ActionGroupRepositoryTestFixture> BuildAsync()
    {
        await Task.WhenAll(_actionGroupTasks);
        return this;
    }
}