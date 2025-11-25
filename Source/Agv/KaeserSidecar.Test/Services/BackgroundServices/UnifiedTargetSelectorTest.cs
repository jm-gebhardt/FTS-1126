using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using MassTransit.Testing;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.BackgroundServices;

[TestClass]
public class UnifiedTargetSelectorTest
{
    #region DP1 (TargetSelector) Tests
    [TestMethod]
    public async Task ExecuteAsync_NoActionEnqueuedForTargetSelection_DoesNotPublishSetTargetForAction()
    {
        var fixture = new UnifiedTargetSelectorTestFixture();

        await fixture.ExecuteAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        await fixture.ShouldNotHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1OneLoop_PublishedSetTargetForActionWithStation1()
    {
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup()
            .WithStationHasPalletJam(null);
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.AsTargetSelector().Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.ExecuteAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(fixture.Stations[0].Id);
        await fixture.ShouldHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1PalletJamAtStation1_PublishedSetTargetForActionWithStation2()
    {
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup()
            .WithStationHasPalletJam(UnifiedTargetSelectorTestFixture.StationNames[0].ToGuid());
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.AsTargetSelector().Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.ExecuteAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(fixture.Stations[1].Id);
        await fixture.ShouldHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1TargetSelected_UpdatesDecisionPointRegistry()
    {
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup()
            .WithStationHasPalletJam(null);
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.AsTargetSelector().Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.ExecuteAsync();

        fixture.DecisionPointRepository.Verify(x => x.UpdateAsync(It.IsAny<DecisionPointAction>()), Times.Once);
        var decisionPointAction = fixture.DecisionPointRepository.Invocations.First().Arguments[0] as DecisionPointAction;
        decisionPointAction.ShouldNotBeNull();
        decisionPointAction.CorrelationId.ShouldBe(moveAction.CorrelationId);
        decisionPointAction.Target.ShouldBe(fixture.Stations[0].Id);
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1LockedStation_SelectsUnlockedStation()
    {
        var agv = Guid.NewGuid();
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithOccupancies(occupiedCountAndAgv: (1, agv))
            .AddDropActionGroup()
            .WithStationHasPalletJam(null);
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.AsTargetSelector().Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.ExecuteAsync();

        fixture.DecisionPointRepository.Verify(x => x.UpdateAsync(It.IsAny<DecisionPointAction>()), Times.Once);
        var decisionPointAction = fixture.DecisionPointRepository.Invocations.First().Arguments[0] as DecisionPointAction;
        decisionPointAction.ShouldNotBeNull();
        decisionPointAction.CorrelationId.ShouldBe(moveAction.CorrelationId);
        decisionPointAction.Target.ShouldBe(fixture.Stations[1].Id);
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1TwoLoopsActionDidNotCompleteInBetween_DoesNotPublishTwice()
    {
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup()
            .WithStationHasPalletJam(null);
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.AsTargetSelector().Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.ExecuteAsync();

        // Second run of loop without dequeue
        await fixture.ExecuteAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(1);

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(fixture.Stations[0].Id);
        await fixture.ShouldHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1OneStationWithTwoActions_SelectsOldestAction()
    {
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithConsideredStations(["1".ToGuid()])
            .AddDropActionGroup()
            .AddPickActionGroup();
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var moveActionOfPick = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.AsTargetSelector().Enqueue(moveAction.CorrelationId, fixture.ActionGroups[0].AssignedAgv!.Value);
        await fixture.AsTargetSelector().Enqueue(moveActionOfPick.CorrelationId, fixture.ActionGroups[1].AssignedAgv!.Value);

        await fixture.ExecuteAsync();

        var setTargetForAction = await fixture.GetFirstPublishedMessageAsync<SetTargetForAction>();
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.ActionId.ShouldBe(moveAction.CorrelationId);
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1ActionEnqueued_OccupiesStation1()
    {
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddDropActionGroup()
            .WithStationHasPalletJam(null);
        var moveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var assignedAgv = fixture.ActionGroups[0].AssignedAgv!.Value;

        await fixture.AsTargetSelector().Enqueue(moveAction.CorrelationId, assignedAgv);
        await fixture.VerifyStationOccupancyAsync(fixture.Stations[0].Id, 0);

        await fixture.ExecuteAsync();

        await fixture.VerifyStationOccupancyAsync(fixture.Stations[0].Id, 1, assignedAgv);
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1ActionInRepository_UsedForTargetSelection()
    {
        var agv = Guid.NewGuid();
        var dropActionGroupMessage = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        dropActionGroupMessage.AssignedAgv = agv;
        var actionGroup = TransportOrder.Test.Helper.AgvActionGenerator.ConvertFromMessage(dropActionGroupMessage);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithDecisionPointAction(new DecisionPointAction(moveAction.CorrelationId, agv, DateTime.Now, ["1".ToGuid(), "2".ToGuid()]))
            .AddActionGroup(actionGroup)
            .WithStationHasPalletJam(null);

        await fixture.ExecuteAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.ActionId.ShouldBe(moveAction.CorrelationId);
        await fixture.ShouldHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1CompletedActionInRepository_NotUsedForTargetSelection()
    {
        var decisionPointAction = new DecisionPointAction(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now, ["1".ToGuid(), "2".ToGuid()]);
        decisionPointAction.SetTarget("finished".ToGuid());
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithDecisionPointAction(decisionPointAction);

        await fixture.ExecuteAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        await fixture.ShouldNotHavePublishedAsync<StationReserved>();
    }
    #endregion

    #region SLW (WaitingHandler) Tests
    [TestMethod]
    public async Task ExecuteAsync_SlwNoActionToHandle_DoesNotPublishSetTargetForAction()
    {
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies();

        await fixture.ExecuteAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        await fixture.ShouldNotHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_SlwStationNotOccupied_PublishedSetTargetForAction()
    {
        var agv = Guid.NewGuid();
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithStationHasPalletJam(null)
            .AddShortCircuitActionGroup(agv);

        var actionGroup = fixture.ActionGroups[0];
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        moveAction.StartedDate = DateTime.UtcNow;

        await fixture.AsWaitingHandler().StartHandling(moveAction.CorrelationId, agv, actionGroup.CreatedDate);
        await fixture.ExecuteAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(UnifiedTargetSelectorTestFixture.ShortLoopTargetId);

        await fixture.VerifyStationOccupancyAsync(UnifiedTargetSelectorTestFixture.ShortLoopTargetId, 1, agv);
        fixture.WaitingPointRepository.Verify(x => x.UpdateAsync(It.IsAny<WaitingPointAction>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_SlwStationHasPalletJam_DoesNotPublishSetTargetForAction()
    {
        var agv = Guid.NewGuid();
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithStationHasPalletJam(UnifiedTargetSelectorTestFixture.ShortLoopTargetId)
            .AddShortCircuitActionGroup(agv);

        var actionGroup = fixture.ActionGroups[0];
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        moveAction.StartedDate = DateTime.UtcNow;

        await fixture.AsWaitingHandler().StartHandling(moveAction.CorrelationId, agv, actionGroup.CreatedDate);
        await fixture.ExecuteAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        await fixture.ShouldNotHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_SlwActionInRepository_UsedForTargetSelection()
    {
        var agv = Guid.NewGuid();
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddShortCircuitActionGroup(agv);

        var actionGroup = fixture.ActionGroups[0];
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        moveAction.StartedDate = DateTime.UtcNow;

        fixture.WithWaitingPointAction(new WaitingPointAction(moveAction.CorrelationId, agv, DateTime.Now));

        await fixture.ExecuteAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.ActionId.ShouldBe(moveAction.CorrelationId);
        await fixture.ShouldHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task ExecuteAsync_SlwCompletedActionInRepository_NotUsedForTargetSelection()
    {
        var waitingPointAction = new WaitingPointAction(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now);
        waitingPointAction.SetTarget(UnifiedTargetSelectorTestFixture.ShortLoopTargetId);
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithWaitingPointAction(waitingPointAction);

        await fixture.ExecuteAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        await fixture.ShouldNotHavePublishedAsync<StationReserved>();
    }

    [TestMethod]
    public async Task StartHanding_SlwAlreadyListed_NotListedAgain()
    {
        var agv = Guid.NewGuid();
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .AddShortCircuitActionGroup(agv);

        var actionGroup = fixture.ActionGroups[0];
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        moveAction.StartedDate = DateTime.UtcNow;

        await fixture.AsWaitingHandler().StartHandling(moveAction.CorrelationId, agv, actionGroup.CreatedDate);
        await fixture.AsWaitingHandler().StartHandling(moveAction.CorrelationId, agv, actionGroup.CreatedDate);

        fixture.WaitingPointRepository.Verify(x => x.AddAsync(It.IsAny<WaitingPointAction>()), Times.Once);
    }
    #endregion

    #region Priority Tests - DP1 vs SLW Competition
    [TestMethod]
    public async Task ExecuteAsync_Dp1AndSlwCompeteForSameStation_Dp1EnteredFirst_Dp1GetsPriority()
    {
        var dp1Agv = Guid.NewGuid();
        var slwAgv = Guid.NewGuid();

        // Set short loop target to UnifiedTargetSelectorTestFixture.StationNames[0] so they compete for the same station
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithShortLoopTarget(UnifiedTargetSelectorTestFixture.StationNames[0])
            .AddDropActionGroup(dp1Agv)
            .AddShortCircuitActionGroup(slwAgv)
            .WithStationHasPalletJam(null);

        var dp1MoveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var slwMoveAction = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);

        // DP1 enters first 
        await Task.Delay(10);
        await fixture.AsTargetSelector().Enqueue(dp1MoveAction.CorrelationId, dp1Agv);
        await Task.Delay(10);
        await fixture.AsWaitingHandler().StartHandling(slwMoveAction.CorrelationId, slwAgv, DateTime.UtcNow);

        await fixture.ExecuteAsync();

        // DP1 should get the station
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.ActionId.ShouldBe(dp1MoveAction.CorrelationId);
        setTargetForAction.TargetNodeId.ShouldBe(UnifiedTargetSelectorTestFixture.StationNames[0].ToGuid());

        await fixture.VerifyStationOccupancyAsync(UnifiedTargetSelectorTestFixture.StationNames[0].ToGuid(), 1, dp1Agv);
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1AndSlwCompeteForSameStation_SlwEnteredFirst_SlwGetsPriority()
    {
        var dp1Agv = Guid.NewGuid();
        var slwAgv = Guid.NewGuid();

        // Set short loop target to UnifiedTargetSelectorTestFixture.StationNames[0] so they compete for the same station
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithShortLoopTarget(UnifiedTargetSelectorTestFixture.StationNames[0])
            .AddDropActionGroup(dp1Agv)
            .AddShortCircuitActionGroup(slwAgv)
            .WithStationHasPalletJam(null);

        var slwMoveAction = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);
        var dp1MoveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);

        // SLW enters first 
        await Task.Delay(10);
        await fixture.AsWaitingHandler().StartHandling(slwMoveAction.CorrelationId, slwAgv, DateTime.UtcNow);
        await Task.Delay(10);
        await fixture.AsTargetSelector().Enqueue(dp1MoveAction.CorrelationId, dp1Agv);

        await fixture.ExecuteAsync();

        // SLW should get the station
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First(m => m.Context.Message.TargetNodeId == UnifiedTargetSelectorTestFixture.StationNames[0].ToGuid()).Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.ActionId.ShouldBe(slwMoveAction.CorrelationId);

        await fixture.VerifyStationOccupancyAsync(UnifiedTargetSelectorTestFixture.StationNames[0].ToGuid(), 1, slwAgv);
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1WouldSelectStation2_SlwTargetIsStation3_BothGetTheirStations()
    {
        var dp1Agv = Guid.NewGuid();
        var slwAgv = Guid.NewGuid();

        // UnifiedTargetSelectorTestFixture.StationNames[0] is occupied, so DP1 should select StationNames[1], SLW targets StationNames[2]
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithOccupancies((1, Guid.NewGuid()), (0, null), (0, null))
            .WithShortLoopTarget(UnifiedTargetSelectorTestFixture.StationNames[2]) // Default
            .AddDropActionGroup(dp1Agv)
            .AddShortCircuitActionGroup(slwAgv)
            .WithStationHasPalletJam(null);

        var dp1MoveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var slwMoveAction = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.AsTargetSelector().Enqueue(dp1MoveAction.CorrelationId, dp1Agv);
        await fixture.AsWaitingHandler().StartHandling(slwMoveAction.CorrelationId, slwAgv, fixture.ActionGroups[1].CreatedDate);

        await fixture.ExecuteAsync();

        // Both should get their respective stations since they don't conflict
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(2);

        var dp1Target = fixture.TestHarness.Published.Select<SetTargetForAction>()
            .First(x => x.Context.Message.ActionId == dp1MoveAction.CorrelationId).Context.Message;
        dp1Target.TargetNodeId.ShouldBe(UnifiedTargetSelectorTestFixture.StationNames[1].ToGuid());

        var slwTarget = fixture.TestHarness.Published.Select<SetTargetForAction>()
            .First(x => x.Context.Message.ActionId == slwMoveAction.CorrelationId).Context.Message;
        slwTarget.TargetNodeId.ShouldBe(UnifiedTargetSelectorTestFixture.StationNames[2].ToGuid());
    }

    [TestMethod]
    public async Task ExecuteAsync_MultipleDp1Actions_OnlyFirstWouldSelectShortLoopTarget_SlwEnteredFirst_SlwGetsPriority()
    {
        var dp1Agv1 = Guid.NewGuid();
        var dp1Agv2 = Guid.NewGuid();
        var slwAgv = Guid.NewGuid();

        // UnifiedTargetSelectorTestFixture.StationNames[0] and StationNames[1] are occupied, so first DP1 would select short loop target
        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithOccupancies((1, Guid.NewGuid()), (1, Guid.NewGuid()), (0, null))
            .WithShortLoopTarget(UnifiedTargetSelectorTestFixture.StationNames[2])
            .AddDropActionGroup(dp1Agv1)
            .AddDropActionGroup(dp1Agv2)
            .AddShortCircuitActionGroup(slwAgv)
            .WithStationHasPalletJam(null);

        var dp1MoveAction1 = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var dp1MoveAction2 = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);
        var slwMoveAction = fixture.ActionGroups[2].Actions.Single(a => a.ActionType == ActionType.Move);

        // SLW enters first, then DP1 actions
        await Task.Delay(10);
        await fixture.AsWaitingHandler().StartHandling(slwMoveAction.CorrelationId, slwAgv, fixture.ActionGroups[2].CreatedDate);
        await Task.Delay(10);
        await fixture.AsTargetSelector().Enqueue(dp1MoveAction1.CorrelationId, dp1Agv1);
        await Task.Delay(10);
        await fixture.AsTargetSelector().Enqueue(dp1MoveAction2.CorrelationId, dp1Agv2);

        await fixture.ExecuteAsync();

        // Only SLW should get PS03
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        List<IPublishedMessage<SetTargetForAction>> setTargetMessages = fixture.TestHarness.Published.Select<SetTargetForAction>().ToList();
        setTargetMessages.Count.ShouldBe(1);

        var slwTarget = setTargetMessages.First().Context.Message;
        slwTarget.ActionId.ShouldBe(slwMoveAction.CorrelationId);
        slwTarget.TargetNodeId.ShouldBe(UnifiedTargetSelectorTestFixture.StationNames[2].ToGuid());

        await fixture.VerifyStationOccupancyAsync(UnifiedTargetSelectorTestFixture.StationNames[2].ToGuid(), 1, slwAgv);
    }

    [TestMethod]
    public async Task ExecuteAsync_Dp1TargetsStation1_WithJam_FallsBackToStation2_SlwTargetsStation3_BothSucceed()
    {
        var dp1Agv = Guid.NewGuid();
        var slwAgv = Guid.NewGuid();

        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithShortLoopTarget(UnifiedTargetSelectorTestFixture.StationNames[2])
            .WithStationHasPalletJam(UnifiedTargetSelectorTestFixture.StationNames[0].ToGuid())
            .AddDropActionGroup(dp1Agv)
            .AddShortCircuitActionGroup(slwAgv);

        var dp1MoveAction = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var slwMoveAction = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);

        await fixture.AsTargetSelector().Enqueue(dp1MoveAction.CorrelationId, dp1Agv);
        await fixture.AsWaitingHandler().StartHandling(slwMoveAction.CorrelationId, slwAgv, fixture.ActionGroups[1].CreatedDate);

        await fixture.ExecuteAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        fixture.TestHarness.Published.Select<SetTargetForAction>().Count().ShouldBe(2);

        var dp1Target = fixture.TestHarness.Published.Select<SetTargetForAction>()
            .First(x => x.Context.Message.ActionId == dp1MoveAction.CorrelationId).Context.Message;
        dp1Target.TargetNodeId.ShouldBe(UnifiedTargetSelectorTestFixture.StationNames[1].ToGuid()); // Skipped UnifiedTargetSelectorTestFixture.StationNames[0] due to jam

        var slwTarget = fixture.TestHarness.Published.Select<SetTargetForAction>()
            .First(x => x.Context.Message.ActionId == slwMoveAction.CorrelationId).Context.Message;
        slwTarget.TargetNodeId.ShouldBe(UnifiedTargetSelectorTestFixture.StationNames[2].ToGuid());
    }

    [TestMethod]
    public async Task ExecuteAsync_ThreeDp1ActionsOneSlw_AllCompeteForStation1_EarliestGetsStation()
    {
        var dp1Agv1 = Guid.NewGuid();
        var dp1Agv2 = Guid.NewGuid();
        var dp1Agv3 = Guid.NewGuid();
        var slwAgv = Guid.NewGuid();

        var fixture = new UnifiedTargetSelectorTestFixture()
            .WithDefaultOccupancies()
            .WithShortLoopTarget(UnifiedTargetSelectorTestFixture.StationNames[0])
            .WithConsideredStations(["1".ToGuid()])
            .AddDropActionGroup(dp1Agv1)
            .AddDropActionGroup(dp1Agv2)
            .AddDropActionGroup(dp1Agv3)
            .AddShortCircuitActionGroup(slwAgv)
            .WithStationHasPalletJam(null);

        var dp1MoveAction1 = fixture.ActionGroups[0].Actions.Single(a => a.ActionType == ActionType.Move);
        var dp1MoveAction2 = fixture.ActionGroups[1].Actions.Single(a => a.ActionType == ActionType.Move);
        var dp1MoveAction3 = fixture.ActionGroups[2].Actions.Single(a => a.ActionType == ActionType.Move);
        var slwMoveAction = fixture.ActionGroups[3].Actions.Single(a => a.ActionType == ActionType.Move);

        // Enqueue in order: DP1-2, SLW, DP1-1, DP1-3
        await Task.Delay(10);
        await fixture.AsTargetSelector().Enqueue(dp1MoveAction2.CorrelationId, dp1Agv2);
        await Task.Delay(10);
        await fixture.AsWaitingHandler().StartHandling(slwMoveAction.CorrelationId, slwAgv, DateTime.UtcNow);
        await Task.Delay(10);
        await fixture.AsTargetSelector().Enqueue(dp1MoveAction1.CorrelationId, dp1Agv1);
        await Task.Delay(10);
        await fixture.AsTargetSelector().Enqueue(dp1MoveAction3.CorrelationId, dp1Agv3);

        await fixture.ExecuteAsync();

        // DP1-2 entered first, so it should get the station
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        List<IPublishedMessage<SetTargetForAction>> setTargetMessages = fixture.TestHarness.Published.Select<SetTargetForAction>().ToList();
        setTargetMessages.Count.ShouldBe(1);

        var target = setTargetMessages.First().Context.Message;
        target.ActionId.ShouldBe(dp1MoveAction2.CorrelationId);
        target.TargetNodeId.ShouldBe(UnifiedTargetSelectorTestFixture.StationNames[0].ToGuid());

        await fixture.VerifyStationOccupancyAsync(UnifiedTargetSelectorTestFixture.StationNames[0].ToGuid(), 1, dp1Agv2);
    }
    #endregion
}