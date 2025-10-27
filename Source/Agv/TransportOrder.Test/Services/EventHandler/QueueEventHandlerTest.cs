using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class QueueEventHandlerTest
{
    [TestMethod]
    public async Task ConsumePickActionCompleted_ValidData_PublishesRemoveAgvFromQueueConsideration()
    {
        var agvId = Guid.NewGuid();

        var fixture = new QueueEventHandlerTestFixture().WithActionGroup(new ActionGroup{ AssignedAgv = agvId });
        await fixture.PublishMessageAsync(new PickActionCompleted(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));

        await fixture.ShouldHaveConsumedAsync<PickActionCompleted>();
        await fixture.ShouldHavePublishedAsync<RemoveAgvFromQueueConsideration>();
    }

    [TestMethod]
    public async Task ConsumeDropActionCompleted_ValidData_PublishesRemoveAgvFromQueueConsideration()
    {
        var agvId = Guid.NewGuid();

        var fixture = new QueueEventHandlerTestFixture().WithActionGroup(new ActionGroup { AssignedAgv = agvId });
        await fixture.PublishMessageAsync(new DropActionCompleted(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));

        await fixture.ShouldHaveConsumedAsync<DropActionCompleted>();
        await fixture.ShouldHavePublishedAsync<RemoveAgvFromQueueConsideration>();
    }

    [TestMethod]
    public async Task ConsumeAgvLastNodeIdChanged_ValidData_PublishesAddAgvToQueueConsideration()
    {
        var fixture = new QueueEventHandlerTestFixture().WithQueue(new Queue());
        await fixture.PublishMessageAsync(new AgvLastNodeIdChanged(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHavePublishedAsync<AddAgvToQueueConsideration>();
    }
}