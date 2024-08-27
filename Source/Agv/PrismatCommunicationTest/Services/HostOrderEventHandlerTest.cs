using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class HostOrderEventHandlerTest
{
    [TestMethod]
    public async Task Consume_DecisionPointPassed_ValidData_PublishesQueuePickDropActionGroupForAgv()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        using HostOrderConsumerTestFixture<HostOrderEventHandler> fixture = new HostOrderConsumerTestFixture<HostOrderEventHandler>()
            .WithNextIncompleteOrderWithUnknownPickTarget(order);

        await fixture.TestHarness.Bus.Publish(new DecisionPointPassed(Guid.NewGuid(), "a target node", false));
        (await fixture.ConsumerHarness.Consumed.Any<DecisionPointPassed>()).ShouldBeTrue();

        await fixture.ShouldHavePublishedAsync<QueuePickDropActionGroupForAgv>();
    }

    [TestMethod]
    public async Task Consume_DecisionPointPassed_NoPendingPick_DoesNotPublishQueuePickDropActionGroupForAgv()
    {
        using HostOrderConsumerTestFixture<HostOrderEventHandler> fixture = new HostOrderConsumerTestFixture<HostOrderEventHandler>()
            .WithNextIncompleteOrderWithUnknownPickTarget(null);

        await fixture.TestHarness.Bus.Publish(new DecisionPointPassed(Guid.NewGuid(), "a target node", false));
        (await fixture.ConsumerHarness.Consumed.Any<DecisionPointPassed>()).ShouldBeTrue();

        await fixture.ShouldNotHavePublishedAsync<QueuePickDropActionGroupForAgv>();
    }

    [TestMethod]
    public async Task Consume_CargoTriggerPointPassed_ValidData_PublishesQueueActionGroupWithUnknownDropTargetForAgv()
    {
        var orderDetails = OrderGenerator.GenerateUnknownDropTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        using HostOrderConsumerTestFixture<HostOrderEventHandler> fixture = new HostOrderConsumerTestFixture<HostOrderEventHandler>()
            .WithNextIncompleteOrderWithUnknownDropTarget(order);

        await fixture.TestHarness.Bus.Publish(new CargoTriggerPointPassed(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<CargoTriggerPointPassed>()).ShouldBeTrue();

        await fixture.ShouldHavePublishedAsync<QueueActionGroupWithUnknownDropTargetForAgv>();
    }

    [TestMethod]
    public async Task Consume_CargoTriggerPointPassed_NoPendingPick_DoesNotPublishQueueActionGroupWithUnknownDropTargetForAgv()
    {
        using HostOrderConsumerTestFixture<HostOrderEventHandler> fixture = new HostOrderConsumerTestFixture<HostOrderEventHandler>()
            .WithNextIncompleteOrderWithUnknownDropTarget(null);

        await fixture.TestHarness.Bus.Publish(new CargoTriggerPointPassed(Guid.NewGuid()));
        (await fixture.ConsumerHarness.Consumed.Any<CargoTriggerPointPassed>()).ShouldBeTrue();

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroupWithUnknownDropTargetForAgv>();
    }

    [TestMethod]
    public async Task Consume_DropActionPassedShortLoopTriggerPoint_NextPickOrderForShortLoopExists_PublishesQueuePickDropActionGroupForAgv()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        HostOrderConsumerTestFixture<HostOrderEventHandler> fixture = new HostOrderConsumerTestFixture<HostOrderEventHandler>()
            .WithNextPickOrderForShortLoopDualOperation(hostOrder);

        var shortLoopTriggerPointPassed = new ShortLoopTriggerPointPassed(Guid.NewGuid(), false);
        await fixture.TestHarness.Bus.Publish(shortLoopTriggerPointPassed);

        await fixture.ShouldHaveConsumedAsync<ShortLoopTriggerPointPassed>();
        await fixture.ShouldHavePublishedAsync<QueuePickDropActionGroupForAgv>();

        var message = fixture.TestHarness.Published.Select<QueuePickDropActionGroupForAgv>().First().Context.Message;
        message.CorrelationId.ShouldBe(hostOrder.CorrelationId);
        OrderGenerator.KaeserConfiguration.StationMapping.TryGetValue(hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node, out string? expectedPickNode);
        expectedPickNode.ShouldNotBeNull();
        message.PickNode.ShouldBe(expectedPickNode);
    }

    [TestMethod]
    public async Task Consume_DropActionPassedShortLoopTriggerPoint_NoNextPickOrderForShortLoopExists_DoesNotPublishQueuePickDropActionGroupForAgv()
    {
        HostOrderConsumerTestFixture<HostOrderEventHandler> fixture = new HostOrderConsumerTestFixture<HostOrderEventHandler>()
            .WithNextPickOrderForShortLoopDualOperation(null);
        var shortLoopTriggerPointPassed = new ShortLoopTriggerPointPassed(Guid.NewGuid(), false);
        await fixture.TestHarness.Bus.Publish(shortLoopTriggerPointPassed);

        await fixture.ShouldHaveConsumedAsync<ShortLoopTriggerPointPassed>();
        await fixture.ShouldNotHavePublishedAsync<QueuePickDropActionGroupForAgv>();
    }
}