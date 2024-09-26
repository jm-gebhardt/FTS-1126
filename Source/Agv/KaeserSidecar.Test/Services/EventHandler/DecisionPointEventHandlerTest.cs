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
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class DecisionPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupiedOrLocked_NextPickAvailable_AgvHasEnoughBattery_SelectsStation1SendsTelegramWithPickStation()
    {
        const string targetStation = "1";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithHostOrder(hostOrder)
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithAgvHasLowBatteryLevel(false);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        fixture.VerifyReportingToConveyors("WE01", true, hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node, Times.Once());

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        setTargetForAction.ShouldNotPickNext.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupiedOrLocked_NextPickAvailable_AgvHasNotEnoughBattery_SelectsStation1SendsTelegramWithoutPickStation()
    {
        const string targetStation = "1";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithHostOrder(hostOrder)
            .WithAgvHasLowBatteryLevel(true);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        fixture.VerifyReportingToConveyors("WE01", true, null, Times.Once());

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        setTargetForAction.ShouldNotPickNext.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_AllStationsNotOccupiedButStation1IsLocked_NextPickAvailable_SelectsStation2SendsTelegramWithPickStation()
    {
        const string targetStation = "2";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var lockedStation = new Station("1", AgvGraph.NodeType.PickDrop, []);
        lockedStation.Lock("user", "reason", "description", LockedOrderHandlingType.Abort);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithStations([
                lockedStation, new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithHostOrder(hostOrder)
            .WithAgvHasLowBatteryLevel(false);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        fixture.VerifyReportingToConveyors("WE02", true, hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node, Times.Once());

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        setTargetForAction.ShouldNotPickNext.ShouldBeFalse();
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupiedOrLocked_NoNextPickAvailable_SelectsStation1SendsTelegramWithoutPickStation(bool hasLowBatteryLevel)
    {
        const string targetStation = "1";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy(targetStation, 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ])
            .WithHostOrder(null)
            .WithAgvHasLowBatteryLevel(hasLowBatteryLevel);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        const string stationName = "WE0" + targetStation;
        fixture.VerifyReportingToConveyors(stationName, true, null, Times.Once());

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        setTargetForAction.ShouldNotPickNext.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station2NotOccupiedOrLocked_PublishesSetTargetForActionWithStation2()
    {
        const string targetStation = "2";

        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 1), new StationOccupancy(targetStation, 0), new StationOccupancy("3", 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 3);

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

        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 1), new StationOccupancy("2", 1), new StationOccupancy(targetStation, 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_OnlyStation3NotOccupiedButIsLocked_PublishesSetTargetForActionWithStationWithLowerOccupancy()
    {
        const string targetStation = "2";
        var lockedStation = new Station("3", AgvGraph.NodeType.PickDrop, []);
        lockedStation.Lock("user", "reason", "description", LockedOrderHandlingType.Abort);

        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 2), new StationOccupancy("2", 1), new StationOccupancy(targetStation, 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                lockedStation
            ]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station3NotOccupied_UseOnlyTwoStations_Station1And2HaveIdenticalOccupancies_PublishesSetTargetForActionWithStation1()
    {
        const string targetStation = "1";

        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy(targetStation, 1), new StationOccupancy("2", 1), new StationOccupancy("3", 0)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 2);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station3NotOccupied_UseOnlyTwoStations_Station1And2HaveIdenticalOccupanciesButStation1IsLocked_PublishesSetTargetForActionWithStation2()
    {
        const string targetStation = "2";
        var lockedStation = new Station("1", AgvGraph.NodeType.PickDrop, []);
        lockedStation.Lock("user", "reason", "description", LockedOrderHandlingType.Abort);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy(targetStation, 1), new StationOccupancy("2", 1), new StationOccupancy("3", 0)])
            .WithStations([
               lockedStation, new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 2);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_AllStationsOccupied_Station2HasLowerOccupancyThan1_PublishesSetTargetForActionWithStation2()
    {
        const string targetStation = "2";
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 2), new StationOccupancy(targetStation, 1), new StationOccupancy("3", 1)])
            .WithStations([
                new Station("1", AgvGraph.NodeType.PickDrop, []), new Station("2", AgvGraph.NodeType.PickDrop, []),
                new Station("3", AgvGraph.NodeType.PickDrop, [])
            ]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 3);

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
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_NotIncompleteActionGroup_DoesNotSelectTargetAndDoesNotSendTelegram()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithHostOrder(hostOrder);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()), new AgvStatus(), null, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_NotPickDropActionGroup_DoesNotSelectTargetAndDoesNotSendTelegram()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithHostOrder(hostOrder);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()), new AgvStatus(), null, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_AllStationsLocked_DoesNotSelectTargetAndDoesNotSendTelegram()
    {
        ICollection<Station> stations = [];
        ICollection<StationOccupancy> occupancies = [];
        foreach (string stationName in new List<string> { "1", "2", "3" })
        {
            var station = new Station(stationName, AgvGraph.NodeType.PickDrop, []);
            station.Lock("user", "reason", "description", LockedOrderHandlingType.Abort);
            stations.Add(station);
            var occupancy = new StationOccupancy(stationName, 0);
            occupancies.Add(occupancy);
        }
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies(occupancies)
            .WithStations(stations);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }
}