using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class EfNodeRepositoryTestFixture : TransportOrderDbFixture, IDisposable
{
    public EfNodeRepository Repository { get; }

    public EfNodeRepositoryTestFixture()
    {
        Repository = new EfNodeRepository(ContextFactory);
    }

    public void AddGraph(EfAgvGraph agvGraph)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<EfAgvGraph>().Add(agvGraph);
        context.SaveChanges();
    }

    public void AddMap(Map map)
    {
        using var context = ContextFactory.CreateDbContext();

        context.Set<EfNode>().AttachRange(map.Nodes);
        context.Set<EfEdge>().AttachRange(map.Edges);
        context.Set<Map>().Add(map);
        context.SaveChanges();
    }

    public void AddNodeLocks(List<NodeLock> nodeLocks)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<NodeLock>().AddRange(nodeLocks);
        context.SaveChanges();
    }
}