using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Queue;

[TestClass]
public class QueueCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_EvacuateQueue_AllAgvsEntered_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue);

        var command = new EvacuateQueue(queue.Name, queue.EntryNode!, 0);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueue>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();

        List<QueueActionGroup> queueActionGroupMessages = fixture.TestHarness.Published.Select<QueueActionGroup>().Select(m => m.Context.Message).ToList();
        queueActionGroupMessages.Count.ShouldBe(queue.AgvFillQuantity);

        foreach (var node in queue.Nodes)
        {
            Guid? agv = node.GetCurrentAgv();
            var message = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == agv);
            message.ShouldNotBeNull();
            message.ActionGroup.IsManual.ShouldBeTrue();
            message.ActionGroup.Actions.Count.ShouldBe(1);
            message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId.ShouldBe(command.EvacuationTarget);
        }
    }

    [TestMethod]
    public async Task Consume_EvacuateQueue_EmptyQueue_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue);

        var command = new EvacuateQueue(queue.Name, queue.EntryNode!, 0);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueue>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_EvacuateQueue_NotAllAgvsEntered_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Free, NodeLockState.Reserved, NodeLockState.Occupied]);
        var queueActionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: queue.EntryNode, assignedAgv: Guid.NewGuid());
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([queueActionGroup]);

        var command = new EvacuateQueue(queue.Name, queue.EntryNode!, 0);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueue>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();

        List<QueueActionGroup> queueActionGroupMessages = fixture.TestHarness.Published.Select<QueueActionGroup>().Select(m => m.Context.Message).ToList();
        queueActionGroupMessages.Count.ShouldBe(queue.AgvFillQuantity + 1);

        foreach (var node in queue.Nodes)
        {
            Guid? agv = node.GetCurrentAgv();
            if (agv == null)
            {
                continue;
            }
            var message = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == agv);
            message.ShouldNotBeNull();
            message.ActionGroup.IsManual.ShouldBeTrue();
            message.ActionGroup.Actions.Count.ShouldBe(1);
            message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId.ShouldBe(command.EvacuationTarget);
        }

        var messageForAgvOnTheWayForQueueing = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == queueActionGroup.AssignedAgv);
        messageForAgvOnTheWayForQueueing.ShouldNotBeNull();
        messageForAgvOnTheWayForQueueing.ActionGroup.IsManual.ShouldBeTrue();
        messageForAgvOnTheWayForQueueing.ActionGroup.Actions.Count.ShouldBe(1);
        messageForAgvOnTheWayForQueueing.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId.ShouldBe(command.EvacuationTarget);
    }

    [TestMethod]
    public async Task Consume_EvacuateQueueHead_EmptyQueue_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([]);

        var command = new EvacuateQueueHead(queue.Name, queue.EntryNode!);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueueHead>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_EvacuateQueueHead_ValidData_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([]);

        var command = new EvacuateQueueHead(queue.Name, queue.EntryNode!);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueueHead>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();

        List<QueueActionGroup> queueActionGroupMessages = fixture.TestHarness.Published.Select<QueueActionGroup>().Select(m => m.Context.Message).ToList();
        queueActionGroupMessages.Count.ShouldBe(1);

        queue.Head.ShouldNotBeNull();

        Guid? agv = queue.Head.GetCurrentAgv();
        var message = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == agv);
        message.ShouldNotBeNull();
        message.ActionGroup.IsManual.ShouldBeTrue();
        message.ActionGroup.Actions.Count.ShouldBe(1);
        message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId.ShouldBe(command.EvacuationTarget);
    }

    [TestMethod]
    public async Task Consume_EvacuateQueueHead_HeadNodeReserved_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Reserved, NodeLockState.Free, NodeLockState.Free]);
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([]);

        var command = new EvacuateQueueHead(queue.Name, queue.EntryNode!);
        await fixture.TestHarness.Bus.Publish(command);

        List<QueueActionGroup> queueActionGroupMessages = fixture.TestHarness.Published.Select<QueueActionGroup>().Select(m => m.Context.Message).ToList();
        queueActionGroupMessages.Count.ShouldBe(1);

        queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv().ShouldNotBeNull();

        Guid? agv = queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv();
        var message = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == agv);
        message.ShouldNotBeNull();
        message.ActionGroup.IsManual.ShouldBeTrue();
        message.ActionGroup.Actions.Count.ShouldBe(1);
        message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId.ShouldBe(command.EvacuationTarget);
    }

    [TestMethod]
    public async Task Consume_EvacuateQueueHead_PreviousMoveAction_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        Guid? agv = queue.Head?.GetCurrentAgv();
        var queueActionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: queue.EntryNode, assignedAgv: agv);
       
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([queueActionGroup]);

        var command = new EvacuateQueueHead(queue.Name, queue.EntryNode!);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueueHead>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }
}