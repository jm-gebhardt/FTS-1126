using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class GraphNodeRepositoryTestFixture : TransportOrderDbFixture, IDisposable
{
    public GraphNodeRepository Repository { get; }

    public GraphNodeRepositoryTestFixture()
    {
        Repository = new GraphNodeRepository(new SemaphoreProvider<INodeStateDbSemaphore>(new Mock<ILogger<SemaphoreProvider<INodeStateDbSemaphore>>>().Object), new Mock<ILogger<GraphNodeRepository>>().Object, ContextFactory);
    }

    protected override IEnumerable<Profile> GetProfiles()
    {
        yield return new GraphProfile();
    }

    public void AddGraph(AgvGraph agvGraph)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<AgvGraph>().Add(agvGraph);
        context.SaveChanges();
    }

    public void AddMap(Map map)
    {
        using var context = ContextFactory.CreateDbContext();

        context.Set<GraphNode>().AttachRange(map.Nodes);
        context.Set<GraphEdge>().AttachRange(map.Edges);
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

    public void AddNode(GraphNode node)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<GraphNode>().Add(node);
        context.SaveChanges();
    }

    public void AddNodes(IEnumerable<GraphNode> nodes)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<GraphNode>().AddRange(nodes);
        context.SaveChanges();
    }

    public void AddNodeLock(NodeLock nodeLock)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Set<NodeLock>().Add(nodeLock);
        context.SaveChanges();
    }

    public NodeLock? GetNodeLock(Guid nodeId)
    {
        using var context = ContextFactory.CreateDbContext();
        return context.Set<NodeLock>().SingleOrDefault(nl => nl.NodeId == nodeId);
    }
}