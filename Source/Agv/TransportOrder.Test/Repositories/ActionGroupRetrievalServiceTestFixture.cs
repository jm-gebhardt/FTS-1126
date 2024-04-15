using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;

public class ActionGroupRetrievalServiceTestFixture : TransportOrderDbFixture
{
    public ActionGroupRetrievalService ActionGroupRetrievalService { get; }

    public ActionGroupRetrievalServiceTestFixture()
    {
        ActionGroupRetrievalService = new ActionGroupRetrievalService(ContextFactory);
    }
}