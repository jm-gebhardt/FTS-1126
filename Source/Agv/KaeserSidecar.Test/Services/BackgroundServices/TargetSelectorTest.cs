using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.BackgroundServices;

[TestClass]
public class TargetSelectorTest
{
    [TestMethod]
    public async Task ExecuteAsync_NoActionEnqueuedForTargetSelection_DoesNotPublish()
    {
        var fixture = new TargetSelectorTestFixture();
        var cancellationToken = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        fixture.Selector.StartAsync(cancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await Task.Delay(100, cancellationToken.Token);
        cancellationToken.Cancel();
        await fixture.Selector.StopAsync(CancellationToken.None);

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
    }

    [TestMethod]
    public async Task ExecuteAsync_Station1NotOccupiedOrLocked_OneLoop_PublishedSetTargetForActionWithStation1()
    {
        const string targetStation = "1";
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = Guid.NewGuid();
        var dropActionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);

        var fixture = new TargetSelectorTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0, null), new StationOccupancy("2", 0, null), new StationOccupancy("3", 0, null)])
            .AddActionGroup(dropActionGroup)
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var moveAction = dropActionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        await fixture.Selector.Enqueue(moveAction.CorrelationId, dropActionGroupMessage.AssignedAgv.Value);

        var cancellationToken = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        fixture.Selector.StartAsync(cancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await Task.Delay(100, cancellationToken.Token);
        cancellationToken.Cancel();
        await fixture.Selector.StopAsync(CancellationToken.None);

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task ExecuteAsync_Station1NotOccupiedOrLocked_TwoLoopsActionDidNotCompleteInbetween_DoesNotPublishTwice()
    {
        const string targetStation = "1";
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = Guid.NewGuid();
        var dropActionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);

        var fixture = new TargetSelectorTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0, null), new StationOccupancy("2", 0, null), new StationOccupancy("3", 0, null)])
            .AddActionGroup(dropActionGroup)
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var moveAction = dropActionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        await fixture.Selector.Enqueue(moveAction.CorrelationId, dropActionGroupMessage.AssignedAgv.Value);

        // First run of loop
        var cancellationToken = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        fixture.Selector.StartAsync(cancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await Task.Delay(100, cancellationToken.Token);
        cancellationToken.Cancel();
        await fixture.Selector.StopAsync(CancellationToken.None);

        // Second run of loop without dequeue
        cancellationToken = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        fixture.Selector.StartAsync(cancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await Task.Delay(100, cancellationToken.Token);
        cancellationToken.Cancel();
        await fixture.Selector.StopAsync(CancellationToken.None);
        
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(1);
        
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task ExecuteAsync_Station1NotOccupiedOrLocked_TwoLoopsWithDifferentActions_PublishesTwice()
    {
        const string targetStation = "1";
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = Guid.NewGuid();
        var dropActionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);

        var pickActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutPickTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        pickActionGroupMessage.AssignedAgv = Guid.NewGuid();
        var pickActionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(pickActionGroupMessage);

        var fixture = new TargetSelectorTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0, null), new StationOccupancy("2", 0, null), new StationOccupancy("3", 0, null)])
            .AddActionGroup(dropActionGroup)
            .AddActionGroup(pickActionGroup)
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var moveAction = dropActionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        await fixture.Selector.Enqueue(moveAction.CorrelationId, dropActionGroupMessage.AssignedAgv.Value);

        // First run of loop
        var cancellationToken = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        fixture.Selector.StartAsync(cancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await Task.Delay(100, cancellationToken.Token);
        cancellationToken.Cancel();
        await fixture.Selector.StopAsync(CancellationToken.None);

        var moveActionOfPick = pickActionGroupMessage.Actions.Single(a => a.ActionType == ActionType.Move);
        await fixture.Selector.Enqueue(moveActionOfPick.CorrelationId, pickActionGroupMessage.AssignedAgv.Value);
       
        // Second run of loop without dequeue
        cancellationToken = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        fixture.Selector.StartAsync(cancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await Task.Delay(100, cancellationToken.Token);
        cancellationToken.Cancel();
        await fixture.Selector.StopAsync(CancellationToken.None);

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(2);

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task ExecuteAsync_Station1NotOccupiedOrLocked_TwoLoopsWithDifferentActionsFirstActionCompleted_PublishesTwice()
    {
        const string targetStation = "1";
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = Guid.NewGuid();
        var dropActionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);

        var pickActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutPickTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        pickActionGroupMessage.AssignedAgv = Guid.NewGuid();
        var pickActionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(pickActionGroupMessage);

        var fixture = new TargetSelectorTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0, null), new StationOccupancy("2", 0, null), new StationOccupancy("3", 0, null)])
            .AddActionGroup(dropActionGroup)
            .AddActionGroup(pickActionGroup)
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var moveAction = dropActionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        await fixture.Selector.Enqueue(moveAction.CorrelationId, dropActionGroupMessage.AssignedAgv.Value);

        // First run of loop
        var cancellationToken = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        fixture.Selector.StartAsync(cancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await Task.Delay(100, cancellationToken.Token);
        cancellationToken.Cancel();
        await fixture.Selector.StopAsync(CancellationToken.None);

        await fixture.Selector.Dequeue(moveAction.CorrelationId);

        var moveActionOfPick = pickActionGroupMessage.Actions.Single(a => a.ActionType == ActionType.Move);
        await fixture.Selector.Enqueue(moveActionOfPick.CorrelationId, pickActionGroupMessage.AssignedAgv.Value);

        await fixture.Selector.Dequeue(moveAction.CorrelationId);
        
        // Second run of loop 
        cancellationToken = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        fixture.Selector.StartAsync(cancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await Task.Delay(100, cancellationToken.Token);
        cancellationToken.Cancel();
        await fixture.Selector.StopAsync(CancellationToken.None);

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(2);

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First( a => a.Context.Message.ActionId == moveAction.CorrelationId).Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }
}



