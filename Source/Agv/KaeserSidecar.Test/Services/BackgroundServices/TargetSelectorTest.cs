using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.BackgroundServices;

[TestClass]
public class TargetSelectorTest
{
    [TestMethod]
    public async Task ExecuteAsync_NoActionEnqueuedForTargetSelection_DoesNotPublishSetTargetForAction()
    {
        var fixture = new TargetSelectorTestFixture();

        await fixture.SelectTargetAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
    }

    [TestMethod]
    public async Task ExecuteAsync_OneLoop_PublishedSetTargetForActionWithStation1()
    {
        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.Selector.Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.SelectTargetAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(fixture.Stations[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_TargetSelected_UpdatesDecisionPointRegistry()
    {
        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.Selector.Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.SelectTargetAsync();

        fixture.DecisionPointRepository.Verify(x => x.UpdateAsync(It.IsAny<DecisionPointAction>()), Times.Once);
        var decisionPointAction = fixture.DecisionPointRepository.Invocations.First().Arguments[0] as DecisionPointAction;
        decisionPointAction.ShouldNotBeNull();
        decisionPointAction.CorrelationId.ShouldBe(moveAction.CorrelationId);
        decisionPointAction.Target.ShouldBe(fixture.Stations[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_LockedStation_SelectsUnlockedStation()
    {
        var agv = Guid.NewGuid();
        var fixture = new TargetSelectorTestFixture()
            .WithOccupancies(occupiedCountAndAgv: (1, agv))
            .AddDropActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.Selector.Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.SelectTargetAsync();

        fixture.DecisionPointRepository.Verify(x => x.UpdateAsync(It.IsAny<DecisionPointAction>()), Times.Once);
        var decisionPointAction = fixture.DecisionPointRepository.Invocations.First().Arguments[0] as DecisionPointAction;
        decisionPointAction.ShouldNotBeNull();
        decisionPointAction.CorrelationId.ShouldBe(moveAction.CorrelationId);
        decisionPointAction.Target.ShouldBe(fixture.Stations[1].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_TwoLoopsActionDidNotCompleteInBetween_DoesNotPublishTwice()
    {
        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.Selector.Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.SelectTargetAsync();

        // Second run of loop without dequeue
        await fixture.SelectTargetAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(1);

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(fixture.Stations[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_TwoLoopsWithDifferentActions_PublishesTwice()
    {
        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup()
            .AddPickActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var moveActionOfPick = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.Selector.Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.SelectTargetAsync();

        await fixture.Selector.Enqueue(moveActionOfPick.CorrelationId, fixture.ActionGroups[1].AssignedAgv!.Value);

        // Second run of loop without dequeue
        await fixture.SelectTargetAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(2);

        var setTargetForAction = await fixture.GetFirstPublishedMessageAsync<SetTargetForAction>();
        setTargetForAction.ShouldNotBeNull();
        Console.WriteLine($"CorrelationId: {setTargetForAction.CorrelationId}, MoveAction: {moveAction.CorrelationId}, MoveActionOfPick: {moveActionOfPick.CorrelationId}");
        setTargetForAction.ActionId.ShouldBe(moveAction.CorrelationId);
        setTargetForAction.TargetNodeId.ShouldBe(fixture.Stations[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_TwoLoopsWithDifferentActionsFirstActionCompleted_PublishesTwice()
    {
        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup()
            .AddPickActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.Selector.Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        // First run of loop
        await fixture.SelectTargetAsync();

        await fixture.Selector.Dequeue(moveAction.CorrelationId);

        var moveActionOfPick = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);
        await fixture.Selector.Enqueue(moveActionOfPick.CorrelationId, fixture.ActionGroups[1].AssignedAgv!.Value);

        await fixture.Selector.Dequeue(moveAction.CorrelationId);

        // Second run of loop 
        await fixture.SelectTargetAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(2);

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First(a => a.Context.Message.ActionId == moveAction.CorrelationId).Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(fixture.Stations[0].Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_OneStationWithTwoActions_SelectsOldestAction()
    {
        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithStationCount(1)
            .AddDropActionGroup()
            .AddPickActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var moveActionOfPick = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.Selector.Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.Selector.Enqueue(moveActionOfPick.CorrelationId, fixture.ActionGroups[1].AssignedAgv!.Value);

        await fixture.SelectTargetAsync();

        var setTargetForAction = await fixture.GetFirstPublishedMessageAsync<SetTargetForAction>();
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.ActionId.ShouldBe(moveAction.CorrelationId);
    }

    [TestMethod]
    public async Task ExecuteAsync_ActionEnqueued_OccupiesStation1()
    {
        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var assignedAgv = fixture.ActionGroups[0].AssignedAgv!.Value;

        await fixture.Selector.Enqueue(moveAction.CorrelationId, assignedAgv);
        await fixture.VerifyStationOccupancyAsync(fixture.Stations[0].Name, 0);

        await fixture.SelectTargetAsync();

        await fixture.VerifyStationOccupancyAsync(fixture.Stations[0].Name, 1, assignedAgv);
    }

    [TestMethod]
    public async Task ExecuteAsync_TwoActionsEnqueued_FirstOccupiesStation1AndSetTargetForActionIsSent()
    {
        var dropAgv = Guid.NewGuid();
        var originalAgvOnStation = Guid.NewGuid();
        var fixture = new TargetSelectorTestFixture()
            .WithStationCount(1)
            .WithOccupancies(occupiedCountAndAgv: (1, originalAgvOnStation))
            .AddDropActionGroup(dropAgv)
            .AddPickActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var moveActionOfPick = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);
        var pickAgv = fixture.ActionGroups[1].AssignedAgv!.Value;

        await fixture.Selector.Enqueue(moveAction.CorrelationId, dropAgv);
        await fixture.Selector.Enqueue(moveActionOfPick.CorrelationId, pickAgv);

        await fixture.SelectTargetAsync();
        await fixture.VerifyStationOccupancyAsync(fixture.Stations[0].Name, 1, originalAgvOnStation);
        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();

        await fixture.FreeStation(fixture.Stations[0].Name, originalAgvOnStation);
        await fixture.SelectTargetAsync();

        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(1);
        await fixture.VerifyStationOccupancyAsync(fixture.Stations[0].Name, 1, dropAgv);
    }

    [TestMethod]
    public async Task ExecuteAsync_ActionInRepository_UsedForTargetSelection()
    {
        var agv = Guid.NewGuid();
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = agv;
        var actionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithDecisionPointAction(new DecisionPointAction(moveAction.CorrelationId, agv, DateTime.Now))
            .AddActionGroup(actionGroup);

        await fixture.SelectTargetAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.ActionId.ShouldBe(moveAction.CorrelationId);
    }

    [TestMethod]
    public async Task ExecuteAsync_CompletedActionInRepository_NotUsedForTargetSelection()
    {
        var decisionPointAction = new DecisionPointAction(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now);
        decisionPointAction.SetTarget("finished");
        var fixture = new TargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithDecisionPointAction(decisionPointAction);

        await fixture.SelectTargetAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
    }
}