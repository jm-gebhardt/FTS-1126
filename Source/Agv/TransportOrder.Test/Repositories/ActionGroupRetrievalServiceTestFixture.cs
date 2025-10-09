using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class ActionGroupRetrievalServiceTestFixture : TransportOrderDbFixture
{
    public ActionGroupRetrievalService ActionGroupRetrievalService { get; }

    public ActionGroupRetrievalServiceTestFixture()
    {
        ActionGroupRetrievalService = new ActionGroupRetrievalService(ContextFactory, MappingHelper.GetMapper(new FilterableProfile(), new FilteredProfile()));
    }
}