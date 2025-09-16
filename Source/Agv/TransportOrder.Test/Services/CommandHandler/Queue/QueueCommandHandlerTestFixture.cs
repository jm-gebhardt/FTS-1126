using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Queue;

public class QueueCommandHandlerTestFixture : MassTransitConsumerTestFixture<QueueCommandHandler>
{
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly SemaphoreProvider<IQueueingSemaphore> _queueSemaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IQueueingSemaphore>>>().Object);

    public QueueCommandHandlerTestFixture()
    {
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

    protected override IEnumerable<Profile> GetProfiles()
    {
        yield return new GraphProfile();
    }

    public QueueCommandHandlerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue queue)
    {
        _queueRepository.Setup(x => x.GetQueueByName(It.IsAny<string>())).ReturnsAsync(queue);
        foreach (var queueNode in queue.Nodes)
        {
            var graphNode = Mapper.Map<GraphNode>(queueNode.Node);
            _agvDb.AddNode(graphNode);
        }
        return this;
    }

    public QueueCommandHandlerTestFixture WithActionGroups(ICollection<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentActionGroupsToTarget(It.IsAny<Guid>())).ReturnsAsync(actionGroups);
        foreach (var actionGroup in actionGroups)
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

    public QueueCommandHandlerTestFixture WithQueueInternalMoves(ICollection<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetNotStartedActionGroupsToTargetsForAgv(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>())).ReturnsAsync((Guid _, ICollection<Guid> targets) => { return actionGroups.Where(a => a.GetTargets().Intersect(targets).Count() != 0).ToList(); });
        return this;
    }
}