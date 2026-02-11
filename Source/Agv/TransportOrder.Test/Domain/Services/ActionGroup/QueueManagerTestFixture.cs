using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.TransportOrder.Core.Services;
using Gebhardt.Acc.TransportOrder.Messages.Events.Queue;
using MassTransit;
using Queue = Gebhardt.Acc.TransportOrder.Domain.Models.Queue.Queue;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

public sealed class QueueManagerTestFixture : FixtureBase
{
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<INodeRepository> _nodeRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IRouteService> _routeService = new();
    private readonly SemaphoreProvider<IQueueingSemaphore> _semaphore = new(new Mock<ILogger<SemaphoreProvider<IQueueingSemaphore>>>().Object);
    private readonly Mock<ILogger<QueueManager>> _logger = new();
    private readonly Mock<IGraphRepository> _graphRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    public readonly QueueManager QueueManager;

    public QueueManagerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _queueRepository.Object);
            s.AddTransient(_ => _nodeRepository.Object);
            s.AddTransient(_ => _routeService.Object);
            s.AddTransient(_ => _graphRepository.Object);
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _actionGroupRepository.Object);
            s.AddTransient(_ => _publishEndpoint.Object);
            s.AddTransient<ISemaphoreProvider<IQueueingSemaphore>>(_ => _semaphore);
            s.AddTransient(_ => _logger.Object);
            s.AddTransient<QueueManager>();
        });
        Build();
        QueueManager = Provider.GetRequiredService<QueueManager>();
        _agvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>?>())).ReturnsAsync([]);
        _agvDb.Setup(x => x.Agvs.UpdateAgvAsync(It.IsAny<Agv>())).Verifiable();
        _nodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>())).ReturnsAsync(true);
        _queueRepository.Setup(x => x.UpdateQueueSize(It.IsAny<Guid>(), It.IsAny<int>())).Verifiable();
        _queueRepository.Setup(x => x.UpdateQueueAsync(It.IsAny<Queue>())).ReturnsAsync(true);
    }

    public QueueManagerTestFixture WithQueues(ICollection<Queue> queues)
    {
        _queueRepository.Setup(x => x.GetQueues()).ReturnsAsync(queues);
        _queueRepository.Setup(x => x.GetOccupiedQueues()).ReturnsAsync(queues);
        _queueRepository.Setup(x => x.GetQueuesWithCapacity()).ReturnsAsync(queues);
        var queue = queues.FirstOrDefault();
        if (queue != null)
        {
            _graphRepository.Setup(x => x.GetNodesByIdsAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync(queue.Nodes.Select(n => n.NodeId.ToGraphNode()).ToList());
            _routeService.Setup(x => x.GetNodeWithLowestCostsAsync(It.IsAny<Agv>(),
                    It.IsAny<IList<GraphNode>>(),
                    It.IsAny<AgvData?>(), It.IsAny<Guid?>()))
                .ReturnsAsync(queue.EntryNode!.Value.ToGraphNode());
        }
        return this;
    }

    public QueueManagerTestFixture WithQueue(Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueForAgv(It.IsAny<Guid>())).ReturnsAsync(queue);
        _queueRepository.Setup(x => x.GetQueueByIdIfExisting(It.IsAny<Guid>())).ReturnsAsync((Guid? id) => queue != null && queue.Id == id ? queue : null);
        return this;
    }

    public QueueManagerTestFixture WithActionGroups(ICollection<TransportOrder.Domain.Models.ActionGroup.ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetCreatedAndStartedActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups.ToList());
        _actionGroupRepository.Setup(x => x.GetNotStartedActionGroupsToTargetsForAgv(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>())).ReturnsAsync(actionGroups.Where(x => x.OrderId.Equals("Queuing")).ToList());
        return this;
    }

    public QueueManagerTestFixture WithQueueByEntryNode(Queue? queue)
    {
        _queueRepository.Setup(x => x.GetQueueByEntryNode(It.IsAny<Guid>())).ReturnsAsync(queue);
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

    public void VerifyQueueWasUpdated(bool updated = true)
    {
        _queueRepository.Verify(x => x.UpdateQueueAsync(It.IsAny<Queue>()), updated ? Times.Once() : Times.Never());
    }

    public void VerifyQueueChangedWasPublished(bool updated = true)
    {
        _publishEndpoint.Verify(x =>
                x.Publish(It.IsAny<QueueChanged>(), It.IsAny<CancellationToken>()),
            updated ? Times.Once() : Times.Never());
    }

    public void VerifyAgvWasUpdated(bool updated = true)
    {
        _agvDb.Verify(x => x.Agvs.UpdateAgvAsync(It.IsAny<Agv>()), updated ? Times.Once() : Times.Never());
    }
}