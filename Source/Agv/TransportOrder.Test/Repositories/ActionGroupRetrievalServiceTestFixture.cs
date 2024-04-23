using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class ActionGroupRetrievalServiceTestFixture : TransportOrderDbFixture
{
    public ActionGroupRetrievalService ActionGroupRetrievalService { get; }

    public ActionGroupRetrievalServiceTestFixture()
    {
        ActionGroupRetrievalService = new ActionGroupRetrievalService(ContextFactory);
    }
}