using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Queue;

public class QueueCommandHandlerTestFixture: MassTransitConsumerTestFixture<QueueCommandHandler>
{
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly SemaphoreProvider<IQueueingSemaphore> _queueSemaphoreProvider = new();

    public QueueCommandHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _actionGroupRepository.Object);
            s.AddTransient(_ => _queueRepository.Object);
            s.AddSingleton<ISemaphoreProvider<IQueueingSemaphore>>(_ => _queueSemaphoreProvider);
        });
        Build();
    }

    public QueueCommandHandlerTestFixture WithQueue(TransportOrder.Domain.Models.Queue.Queue queue)
    {
        _queueRepository.Setup(x => x.GetQueueByName(It.IsAny<string>())).ReturnsAsync(queue);
        return this;
    }

    public QueueCommandHandlerTestFixture WithActionGroups(ICollection<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentActionGroupsToTarget(It.IsAny<string>())).ReturnsAsync(actionGroups);
        return this;
    }

    public QueueCommandHandlerTestFixture WithQueueInternalMoves(ICollection<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetNotStartedActionGroupsToTargetsForAgv(It.IsAny<Guid>(), It.IsAny<ICollection<string>>())).ReturnsAsync(actionGroups);
        return this;
    }
}