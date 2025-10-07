using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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

        var command = new EvacuateQueue(queue.Name, queue.EntryNode!.Value, 0);
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
            var targetNode = message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.Node;
            targetNode!.Id.ShouldBe(command.EvacuationTargetNodeId);
        }
    }

    [TestMethod]
    public async Task Consume_EvacuateQueue_EmptyQueue_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue);

        var command = new EvacuateQueue(queue.Name, queue.EntryNode!.Value, 0);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueue>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_EvacuateQueue_NotAllAgvsEntered_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Free, NodeLockState.Reserved, NodeLockState.Occupied]);
        string queueEntryNodeName = queue.Nodes.Single(n => n.NodeId == queue.EntryNode).Node!.Name;
        var queueActionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: queueEntryNodeName, assignedAgv: Guid.NewGuid());
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([queueActionGroup]);

        var command = new EvacuateQueue(queue.Name, queue.EntryNode!.Value, 0);
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
            message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.Node!.Id.ShouldBe(command.EvacuationTargetNodeId);
        }

        var messageForAgvOnTheWayForQueueing = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == queueActionGroup.AssignedAgv);
        messageForAgvOnTheWayForQueueing.ShouldNotBeNull();
        messageForAgvOnTheWayForQueueing.ActionGroup.IsManual.ShouldBeTrue();
        messageForAgvOnTheWayForQueueing.ActionGroup.Actions.Count.ShouldBe(1);
        messageForAgvOnTheWayForQueueing.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.Node!.Id.ShouldBe(command.EvacuationTargetNodeId);
    }

    [TestMethod]
    public async Task Consume_EvacuateQueueHead_EmptyQueue_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([]);

        var command = new EvacuateQueueHead(queue.Name, queue.EntryNode!.Value);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueueHead>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_EvacuateQueueHead_ValidData_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var moveToHead = AgvActionGenerator.GenerateMoveActionGroup(queue.Head?.Node?.Name);
        var moveToCenter = AgvActionGenerator.GenerateMoveActionGroup(queue.Nodes.Single(n => n.Position == 1).Node!.Name);
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([])
            .WithQueueInternalMoves([moveToHead, moveToCenter]);

        var command = new EvacuateQueueHead(queue.Name, queue.EntryNode!.Value);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueueHead>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();

        ICollection<QueueActionGroup> queueActionGroupMessages = fixture.GetPublishedMessages<QueueActionGroup>();
        queueActionGroupMessages.Count.ShouldBe(1);

        queue.Head.ShouldNotBeNull();

        Guid? agv = queue.Head.GetCurrentAgv();
        var message = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == agv);
        message.ShouldNotBeNull();
        message.ActionGroup.IsManual.ShouldBeTrue();
        message.ActionGroup.Actions.Count.ShouldBe(1);
        message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.Node!.Id.ShouldBe(command.EvacuationTargetId);

        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
        ICollection<CancelOrAbortActionGroup> cancelOrAbortActionGroupMessages = fixture.GetPublishedMessages<CancelOrAbortActionGroup>();
        cancelOrAbortActionGroupMessages.Count.ShouldBe(1);
        cancelOrAbortActionGroupMessages.Single().CorrelationId.ShouldBe(moveToCenter.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_EvacuateQueueHead_HeadNodeReserved_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Reserved, NodeLockState.Free, NodeLockState.Free]);
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([])
            .WithQueueInternalMoves([]);

        var command = new EvacuateQueueHead(queue.Name, queue.EntryNode!.Value);
        await fixture.TestHarness.Bus.Publish(command);

        List<QueueActionGroup> queueActionGroupMessages = fixture.TestHarness.Published.Select<QueueActionGroup>().Select(m => m.Context.Message).ToList();
        queueActionGroupMessages.Count.ShouldBe(1);

        queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv().ShouldNotBeNull();

        Guid? agv = queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv();
        var message = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == agv);
        message.ShouldNotBeNull();
        message.ActionGroup.IsManual.ShouldBeTrue();
        message.ActionGroup.Actions.Count.ShouldBe(1);
        message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.Node!.Id.ShouldBe(command.EvacuationTargetId);

        await fixture.ShouldNotHavePublishedAsync<CancelOrAbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_EvacuateQueueHead_PreviousMoveAction_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        Guid? agv = queue.Head?.GetCurrentAgv();
        string queueEntryNodeName = queue.Nodes.Single(n => n.NodeId == queue.EntryNode).Node!.Name;
        var queueActionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: queueEntryNodeName, assignedAgv: agv);

        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([queueActionGroup]);

        var command = new EvacuateQueueHead(queue.Name, queue.EntryNode!.Value);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<EvacuateQueueHead>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_EmptyQueue_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([]);

        var command = new EvacuateOrAssignQueueHead(queue.Name, queue.EntryNode!.Value, Guid.NewGuid(), "APL1");
        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(command);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryAssignDelayedOrderToAgv>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_NoOrderToAssign_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var moveToHead = AgvActionGenerator.GenerateMoveActionGroup(queue.Head?.Node?.Name);
        var moveToCenter = AgvActionGenerator.GenerateMoveActionGroup(queue.Nodes.Single(n => n.Position == 1).Node!.Name);
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([])
            .WithQueueInternalMoves([moveToHead, moveToCenter])
            .WithNode(new GraphNode("APL1".ToGuid(), "APL1"));

        var command = new EvacuateOrAssignQueueHead(queue.Name, queue.EntryNode!.Value, Guid.NewGuid(), "APL1");
        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(command);

        ICollection<QueueActionGroup> queueActionGroupMessages = fixture.GetPublishedMessages<QueueActionGroup>();
        queueActionGroupMessages.Count.ShouldBe(1);

        queue.Head.ShouldNotBeNull();

        Guid? agv = queue.Head.GetCurrentAgv();
        var message = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == agv);
        message.ShouldNotBeNull();
        message.ActionGroup.IsManual.ShouldBeTrue();
        message.ActionGroup.Actions.Count.ShouldBe(1);
        message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.Node!.Id.ShouldBe(command.EvacuationTargetId);

        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
        ICollection<CancelOrAbortActionGroup> cancelOrAbortActionGroupMessages = fixture.GetPublishedMessages<CancelOrAbortActionGroup>();
        cancelOrAbortActionGroupMessages.Count.ShouldBe(1);
        cancelOrAbortActionGroupMessages.Single().CorrelationId.ShouldBe(moveToCenter.CorrelationId);
        await fixture.ShouldHavePublishedAsync<TryAssignDelayedOrderToAgv>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_OrderToAssign_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var moveToHead = AgvActionGenerator.GenerateMoveActionGroup(queue.Head?.Node?.Name);
        var moveToCenter = AgvActionGenerator.GenerateMoveActionGroup(queue.Nodes.Single(n => n.Position == 1).Node!.Name);
        var fixture = new QueueCommandHandlerTestFixture(orderToAssign: Guid.NewGuid())
            .WithQueue(queue)
            .WithActionGroups([])
            .WithQueueInternalMoves([moveToHead, moveToCenter])
            .WithNode(new GraphNode("APL1".ToGuid(), "APL1"));

        var command = new EvacuateOrAssignQueueHead(queue.Name, queue.EntryNode!.Value, Guid.NewGuid(), "APL1");
        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(command);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CancelOrAbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryAssignDelayedOrderToAgv>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_HeadNodeReserved_NoOrderToAssign_PublishesQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Reserved, NodeLockState.Free, NodeLockState.Free]);
        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([])
            .WithQueueInternalMoves([])
            .WithNode(new GraphNode("APL1".ToGuid(), "APL1"));

        var command = new EvacuateOrAssignQueueHead(queue.Name, queue.EntryNode!.Value, Guid.NewGuid(), "APL1");
        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(command);

        List<QueueActionGroup> queueActionGroupMessages = fixture.TestHarness.Published.Select<QueueActionGroup>().Select(m => m.Context.Message).ToList();
        queueActionGroupMessages.Count.ShouldBe(1);

        queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv().ShouldNotBeNull();

        Guid? agv = queue.Nodes.MinBy(n => n.Position)?.GetCurrentAgv();
        var message = queueActionGroupMessages.SingleOrDefault(m => m.ActionGroup.AssignedAgv == agv);
        message.ShouldNotBeNull();
        message.ActionGroup.IsManual.ShouldBeTrue();
        message.ActionGroup.Actions.Count.ShouldBe(1);
        message.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.Node!.Id.ShouldBe(command.EvacuationTargetId);

        await fixture.ShouldNotHavePublishedAsync<CancelOrAbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryAssignDelayedOrderToAgv>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_PreviousEvacuation_NoOrderToAssign_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        Guid? agv = queue.Head?.GetCurrentAgv();
        string queueEntryNodeName = queue.Nodes.Single(n => n.NodeId == queue.EntryNode).Node!.Name;
        var queueActionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: queueEntryNodeName, assignedAgv: agv);

        var fixture = new QueueCommandHandlerTestFixture()
            .WithQueue(queue)
            .WithActionGroups([queueActionGroup])
            .WithNode(new GraphNode("APL1".ToGuid(), "APL1"));

        var command = new EvacuateOrAssignQueueHead(queue.Name, queue.EntryNode!.Value, Guid.NewGuid(), "APL1");
        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(command);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CancelOrAbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryAssignDelayedOrderToAgv>();
    }

    [TestMethod]
    public async Task Consume_EvacuateOrAssignQueueHead_PreviousEvacuation_OrderToAssign_DoesNotPublishQueueActionGroup()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        Guid? agv = queue.Head?.GetCurrentAgv();
        string queueEntryNodeName = queue.Nodes.Single(n => n.NodeId == queue.EntryNode).Node!.Name;
        var queueActionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: queueEntryNodeName, assignedAgv: agv);

        var fixture = new QueueCommandHandlerTestFixture(orderToAssign: Guid.NewGuid())
            .WithQueue(queue)
            .WithActionGroups([queueActionGroup])
            .WithNode(new GraphNode("APL1".ToGuid(), "APL1"));

        var command = new EvacuateOrAssignQueueHead(queue.Name, queue.EntryNode!.Value, Guid.NewGuid(), "APL1");
        await fixture.PublishMessageAsync<EvacuateOrAssignQueueHead, string>(command);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CancelOrAbortActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryAssignDelayedOrderToAgv>();
    }
}