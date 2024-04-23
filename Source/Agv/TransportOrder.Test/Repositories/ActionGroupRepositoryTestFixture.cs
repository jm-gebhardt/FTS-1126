using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class ActionGroupRepositoryTestFixture : TransportOrderDbFixture
{
    public EfActionGroupRepository ActionGroupRepository { get; }

    public ActionGroupRepositoryTestFixture()
    {
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
        AddInitTask(ActionGroupRepository.AddAsync(actionGroup));
        return this;
    }

    private new void Build()
    {
        base.Build();
    }

    public new async Task<ActionGroupRepositoryTestFixture> BuildAsync()
    {
        await base.BuildAsync();
        Build();
        return this;
    }
}