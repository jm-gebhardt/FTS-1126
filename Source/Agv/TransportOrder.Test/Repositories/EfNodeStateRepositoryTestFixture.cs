using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class GraphNodeStateRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly GraphNodeStateRepository Repository;

    public GraphNodeStateRepositoryTestFixture()
    {
        var mockLogger = new Mock<ILogger<GraphNodeStateRepository>>();

        Repository = new GraphNodeStateRepository(ContextFactory, new SemaphoreProvider<INodeStateDbSemaphore>(new Mock<ILogger<SemaphoreProvider<INodeStateDbSemaphore>>>().Object), Mapper, mockLogger.Object);
    }

    protected override IEnumerable<Profile> GetProfiles()
    {
        yield return new GraphProfile();
    }

    public NodeLock? Get(Guid nodeId)
    {
        using var context = ContextFactory.CreateDbContext();
        return context.Set<NodeLock>().AsNoTracking().SingleOrDefault(n => n.NodeId == nodeId);
    }

    public GraphNodeStateRepositoryTestFixture WithNodeLock(NodeLock nodeLock)
    {
        Add(nodeLock);
        return WithNode(nodeLock.Node);
    }

    public GraphNodeStateRepositoryTestFixture WithNode(GraphNode node)
    {
        var efNode = Mapper.Map<GraphNode>(node);
        Add(efNode);
        return this;
    }
}