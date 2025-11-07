using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Queue;

public class QueueCommandHandlerTestFixture : MassTransitConsumerTestFixture<QueueCommandHandler>
{
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly SemaphoreProvider<IQueueingSemaphore> _queueSemaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IQueueingSemaphore>>>().Object);

    private static Guid? _orderToAssign;

    public QueueCommandHandlerTestFixture(Guid? orderToAssign = null)
    {
        _orderToAssign = orderToAssign;
        _agvDb.SetupNodeFallbacks();
        AddServices(s =>
        {
            s.AddTransient(_ => _actionGroupRepository.Object);
            s.AddTransient(_ => _queueRepository.Object);
            s.AddTransient(_ => _agvDb.Object);
            s.AddSingleton<ISemaphoreProvider<IQueueingSemaphore>>(_ => _queueSemaphoreProvider);
        });
        Build();
    }

    [UsedImplicitly]
    private class MockRequestConsumer : IConsumer<TryAssignDelayedOrderToAgv>
    {
        public async Task Consume(ConsumeContext<TryAssignDelayedOrderToAgv> context)
        {
            await context.RespondAsync(new AssignedDelayedOrder(_orderToAssign));
        }
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddConsumer<MockRequestConsumer>();
    }

    protected override IEnumerable<Profile> GetProfiles()
    {
        yield return new GraphProfile();
    }

    public QueueCommandHandlerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue queue)
    {
        _queueRepository.Setup(x => x.GetQueueByName(It.IsAny<string>())).ReturnsAsync(queue);
        _queueRepository.Setup(x => x.GetQueueById(It.IsAny<Guid>())).ReturnsAsync(queue);
        _queueRepository.Setup(x => x.GetQueues()).ReturnsAsync([queue]);
        foreach (var queueNode in queue.Nodes)
        {
            var graphNode = Mapper.Map<GraphNode>(queueNode.Node);
            _agvDb.AddNode(graphNode);
        }
        return this;
    }

    public QueueCommandHandlerTestFixture WithQueues(List<TransportOrder.Domain.Models.Queue.Queue> queues)
    {
        _queueRepository.Setup(x => x.GetQueues()).ReturnsAsync(queues);
        foreach (var queue in queues)
        {
            _queueRepository.Setup(x => x.GetQueueByName(queue.Name)).ReturnsAsync(queue);
            _queueRepository.Setup(x => x.GetQueueById(queue.Id)).ReturnsAsync(queue);
            foreach (var queueNode in queue.Nodes)
            {
                var graphNode = Mapper.Map<GraphNode>(queueNode.Node);
                _agvDb.AddNode(graphNode);
            }
        }
        return this;
    }

    public QueueCommandHandlerTestFixture WithNode(GraphNode node)
    {
        _agvDb.AddNode(node);
        return this;
    }

    public QueueCommandHandlerTestFixture WithActionGroups(ICollection<ActionGroup> picks, ICollection<ActionGroup> evacuationMoves, Guid station, Guid evacuationTarget)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentActionGroupsToTarget(It.IsAny<Guid>())).ReturnsAsync((Guid s) =>
        {
            if (s == station)
            {
                return picks;
            }
            if (s == evacuationTarget)
            {
                return evacuationMoves;
            }
            return [];
        });
        foreach (var actionGroup in picks)
        {
            foreach (var action in actionGroup.Actions)
            {
                var targetNode = action.Target.Node;
                if (targetNode != null)
                {
                    _agvDb.AddNode(targetNode);
                }
            }
        }
        foreach (var actionGroup in evacuationMoves)
        {
            foreach (var action in actionGroup.Actions)
            {
                var targetNode = action.Target.Node;
                if (targetNode != null)
                {
                    _agvDb.AddNode(targetNode);
                }
            }
        }
        return this;
    }

    public QueueCommandHandlerTestFixture WithCurrentAction(AgvAction action)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(action);
        return this;
    }

    public QueueCommandHandlerTestFixture WithQueueInternalMoves(ICollection<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetNotStartedActionGroupsToTargetsForAgv(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>())).ReturnsAsync((Guid _, ICollection<Guid> targets) => { return actionGroups.Where(a => a.GetTargets().Intersect(targets).Count() != 0).ToList(); });
        return this;
    }

    public QueueCommandHandlerTestFixture WithNextPicksAtTarget(ICollection<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetNextPicksAtTarget(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>())).ReturnsAsync(actionGroups);
        return this;
    }

    public void ShouldHaveUpdatedQueue(Guid queueId)
    {
        Should.NotThrow(() => _queueRepository.Verify(qr => qr.UpdateQueueAsync(It.Is<TransportOrder.Domain.Models.Queue.Queue>(q => q.Id == queueId))));
    }
}