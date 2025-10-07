using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class AgvEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AllAgvsOffline_ValidData_PublishesSetNotReady()
    {
        using var fixture = new HostOrderConsumerTestFixture<AgvEventHandler>();

        await fixture.TestHarness.Bus.Publish(new AllAgvsOffline());

        await fixture.ShouldHaveConsumedAsync<AllAgvsOffline>();
        await fixture.ShouldHavePublishedAsync<SetNotReady>();
    }

    [TestMethod]
    public async Task Consume_AgvWentOnline_ValidData_PublishesSetReady()
    {
        using var fixture = new HostOrderConsumerTestFixture<AgvEventHandler>();

        await fixture.TestHarness.Bus.Publish(new AgvWentOnline(Guid.NewGuid()));

        await fixture.ShouldHaveConsumedAsync<AgvWentOnline>();
        await fixture.ShouldHavePublishedAsync<SetReady>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_HostOrderNotConsideredForDoppelspiel_PublishesConsiderForDoppelspiel()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        order.CanRunDoppelspiel.ShouldBeFalse();
        using HostOrderConsumerTestFixture<AgvEventHandler> fixture = new HostOrderConsumerTestFixture<AgvEventHandler>()
            .WithCurrentOrderForAgv(order);

        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(new EvacuateOrAssignQueueHead("AplQueue", Guid.NewGuid(), Guid.NewGuid(), "APL1"));

        await fixture.ShouldHavePublishedAsync<ConsiderForDoppelspiel>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_HostOrderAlreadyConsideredForDoppelspiel_DoesNotPublishConsiderForDoppelspiel()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        order.ConsiderForDoppelspiel();
        using HostOrderConsumerTestFixture<AgvEventHandler> fixture = new HostOrderConsumerTestFixture<AgvEventHandler>()
            .WithCurrentOrderForAgv(order);

        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(new EvacuateOrAssignQueueHead("AplQueue", Guid.NewGuid(), Guid.NewGuid(), "APL1"));

        await fixture.ShouldNotHavePublishedAsync<ConsiderForDoppelspiel>();
    }

    [TestMethod]
    public async Task Consume_AgvLeftDoppelspielTrigger_ValidData_QueueUnmatchedDoppelspielPicks()
    {
        using var fixture = new HostOrderConsumerTestFixture<AgvEventHandler>();

        await fixture.PublishMessageAsync(new AgvLeftDoppelspielTrigger(Guid.NewGuid(), "APL1"));

        await fixture.ShouldHavePublishedAsync<QueueUnmatchedDoppelspielPicks>();
    }
}