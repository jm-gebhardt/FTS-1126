using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

public class QueueManagerTestFixture: FixtureBase
{
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IRouteService> _routeService = new();
    private readonly SemaphoreProvider<IQueueingSemaphore> _semaphore = new();
    private readonly Mock<ILogger<QueueManager>> _logger = new();
    private readonly Mock<IGraphRepository> _graphRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    public readonly QueueManager QueueManager;

    public QueueManagerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _queueRepository.Object);
            s.AddTransient(_ => _nodeStateRepository.Object);
            s.AddTransient(_ => _routeService.Object);
            s.AddTransient(_ => _graphRepository.Object);
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _actionGroupRepository.Object);
            s.AddTransient<ISemaphoreProvider<IQueueingSemaphore>>(_ => _semaphore);
            s.AddTransient(_ => _logger.Object);
            s.AddTransient<QueueManager>();
        });
        Build();
        QueueManager = Provider.GetRequiredService<QueueManager>();
        _agvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>?>())).ReturnsAsync([]);
        _agvDb.Setup(x => x.Agvs.UpdateAgvAsync(It.IsAny<Agv>())).Verifiable();
         _nodeStateRepository.Setup(x => x.FilterFreeUnlockedNodes(It.IsAny<IEnumerable<GraphNode>>())).Returns([]);
        _nodeStateRepository.Setup(x => x.ReserveNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);
        _nodeStateRepository.Setup(x => x.ForceOccupyNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);
        _nodeStateRepository.Setup(x => x.ForceReserveNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);
        _queueRepository.Setup(x => x.UpdateQueueSize(It.IsAny<Guid>(), It.IsAny<int>())).Verifiable();
        _queueRepository.Setup(x => x.UpdateQueueAsync(It.IsAny<Queue>())).Verifiable();
    }

    public QueueManagerTestFixture WithQueues(ICollection<Queue> queues)
    {
        _queueRepository.Setup(x => x.GetOccupiedQueues()).ReturnsAsync(queues);
        _queueRepository.Setup(x => x.GetQueuesWithCapacity()).ReturnsAsync(queues);
        return this;
    }

    public QueueManagerTestFixture WithQueue(Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueForAgv(It.IsAny<Guid>())).ReturnsAsync(queue);
        return this;
    }

    public QueueManagerTestFixture WithQueueByEntryNode(Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueByEntryNode(It.IsAny<string>())).ReturnsAsync(queue);
        return this;
    }

    public QueueManagerTestFixture WithAgv(Agv agv)
    {
        var agvData = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        _agvDb.Setup(x => x.Data.GetAgvDataAsync(agv.CorrelationId)).ReturnsAsync(agvData);
        _agvDb.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        _agvDb.Setup(x => x.Agvs.UpdateAgvAsync(It.IsAny<Agv>())).ReturnsAsync(true);
        return this;
    }

    public void VerifyQueueWasUpdated(bool updated=true)
    {
        _queueRepository.Verify(x => x.UpdateQueueAsync(It.IsAny<Queue>()), updated ? Times.Once() : Times.Never());
    }

    public void VerifyAgvWasUpdated(bool updated = true)
    {
        _agvDb.Verify(x => x.Agvs.UpdateAgvAsync(It.IsAny<Agv>()), updated ? Times.Once() : Times.Never());
    }
}