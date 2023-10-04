using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class ActionGroupRetrievalServiceTestFixture : DbTestFixtureBase<TransportOrderDbContext>
{
    public ActionGroupRetrievalService ActionGroupRetrievalService { get; }

    public ActionGroupRetrievalServiceTestFixture()
    {
        ActionGroupRetrievalService = new ActionGroupRetrievalService(ContextFactory);
    }
}