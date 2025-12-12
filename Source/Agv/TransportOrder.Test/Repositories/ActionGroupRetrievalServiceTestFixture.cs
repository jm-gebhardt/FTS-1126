using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class ActionGroupRetrievalServiceTestFixture : ActionGroupRepositoryTestFixture
{
    public ActionGroupRetrievalService ActionGroupRetrievalService { get; }

    public ActionGroupRetrievalServiceTestFixture()
    {
        ActionGroupRetrievalService = new ActionGroupRetrievalService(ContextFactory, MappingHelper.GetMapper(new FilterableProfile(), new FilteredProfile()));
    }

    public new ActionGroupRetrievalServiceTestFixture WithActionGroups(IEnumerable<ActionGroup> actionGroups)
    {
        base.WithActionGroups(actionGroups);
        return this;
    }

    public new async Task<ActionGroupRetrievalServiceTestFixture> BuildAsync()
    {
        await base.BuildAsync();
        return this;
    }
}