using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Test.Helper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class DecisionPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupiedOrLocked_NextPickAvailable_AgvHasEnoughBattery_SelectsStation1()
    {
        const string targetStation = "1";
        var dropActionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState:ActionGroupState.Started, actionState:ActionState.Started);
        var currentHostOrder = OrderGenerator.GenerateOrderFromActionGroup(dropActionGroup);
        var pickActionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget();
        var nextPickOrder = OrderGenerator.GenerateOrderFromActionGroup(pickActionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithCurrentHostOrder(currentHostOrder)
            .WithNextPickOrder(nextPickOrder)
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithAgvHasLowBatteryLevel(false);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), dropActionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupiedOrLocked_NoNextPickAvailable_AgvHasNotEnoughBattery_SelectsStation1()
    {
        const string targetStation = "1";
        var dropActionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        var currentHostOrder = OrderGenerator.GenerateOrderFromActionGroup(dropActionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithCurrentHostOrder(currentHostOrder)
            .WithNextPickOrder(null)
            .WithAgvHasLowBatteryLevel(true);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), dropActionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_AllStationsNotOccupiedButStation1IsLocked_NextPickAvailable_SelectsStation2()
    {
        const string targetStation = "2";
        var dropActionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        var currentHostOrder = OrderGenerator.GenerateOrderFromActionGroup(dropActionGroup);
        var pickActionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget();
        var nextPickOrder = OrderGenerator.GenerateOrderFromActionGroup(pickActionGroup);
        var lockedStation = new Station("1", AgvGraph.NodeType.PickDrop, []);
        lockedStation.Lock("user", "reason", "description", LockedOrderHandlingType.Abort);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithStations([
                lockedStation, new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithCurrentHostOrder(currentHostOrder)
            .WithNextPickOrder(nextPickOrder)
            .WithAgvHasLowBatteryLevel(false);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), dropActionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupiedOrLocked_NoNextPickAvailable_SelectsStation1(bool hasLowBatteryLevel)
    {
        const string targetStation = "1";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        var currentHostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy(targetStation, 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithCurrentHostOrder(currentHostOrder)
            .WithNextPickOrder(null)
            .WithAgvHasLowBatteryLevel(hasLowBatteryLevel);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station2NotOccupiedOrLocked_PublishesSetTargetForActionWithStation2()
    {
        const string targetStation = "2";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        var currentHostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 1), new StationOccupancy(targetStation, 0), new StationOccupancy("3", 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithCurrentHostOrder(currentHostOrder)
            .WithNextPickOrder(null);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_OnlyStation3NotOccupied_PublishesSetTargetForActionWithStation3()
    {
        const string targetStation = "3";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        var currentHostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 1), new StationOccupancy("2", 1), new StationOccupancy(targetStation, 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithCurrentHostOrder(currentHostOrder)
            .WithNextPickOrder(null);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_NoActionGroup_DoesNotSelectTargetAndDoesNotSendTelegram()
    {
        var fixture = new DecisionPointEventHandlerTestFixture();
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()), new AgvStatus(), null, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_NotIncompleteActionGroup_DoesNotSelectTargetAndDoesNotSendTelegram()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(actionGroupState: ActionGroupState.Started, actionState: ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithCurrentHostOrder(hostOrder)
            .WithNextPickOrder(null);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()), new AgvStatus(), null, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_NotPickDropActionGroup_DoesNotSelectTarget()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithCurrentHostOrder(hostOrder)
            .WithNextPickOrder(null);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()), new AgvStatus(), null, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
    }
}