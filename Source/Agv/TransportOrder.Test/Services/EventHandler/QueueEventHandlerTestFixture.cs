using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

public class QueueEventHandlerTestFixture : MassTransitConsumerTestFixture<QueueEventHandler>
{
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    private static IEnumerable<Profile> Profiles => [new AgvStatusProfile()];

    protected override IEnumerable<Profile> GetProfiles() => Profiles;

    protected override MassTransitConsumerTestFixture<QueueEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _queueRepository.Object);
        return this;
    }

    public QueueEventHandlerTestFixture()
    {
        Build();
    }

    public QueueEventHandlerTestFixture WithActionGroup(ActionGroup actionGroup)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        return this;
    }

    public QueueEventHandlerTestFixture WithQueue(Queue queue)
    {
        _queueRepository.Setup(x => x.GetQueueByEntryNode(It.IsAny<Guid>())).ReturnsAsync(queue);
        return this;
    }
}