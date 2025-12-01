using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class GraphNodeRepositoryTestFixture : TransportOrderDbFixture, IDisposable
{
    public GraphNodeRepository Repository { get; }

    public GraphNodeRepositoryTestFixture()
    {
        Repository = new GraphNodeRepository(ContextFactory, Mapper);
    }

    protected override IEnumerable<Profile> GetProfiles()
    {
        yield return new GraphProfile();
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

        context.Set<GraphNode>().AttachRange(map.Nodes);
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

    public void AddStations(List<Station> stations)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<Station>().AddRange(stations);
        context.SaveChanges();
    }
}