using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class DecisionPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupied_NextPickAvailable_AgvHasEnoughBattery_SelectsStation1SendsTelegramWithPickStation()
    {
        const string targetStation = "1";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithHostOrder(hostOrder)
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
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupied_NextPickAvailable_AgvHasNotEnoughBattery_SelectsStation1SendsTelegramWithoutPickStation()
    {
        const string targetStation = "1";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
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

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupied_NoNextPickAvailable_SelectsStation1SendsTelegramWithoutPickStation(bool hasLowBatteryLevel)
    {
        const string targetStation = "1";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy(targetStation, 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
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
    public async Task Consume_AgvPassedDecisionPoint1_Station2NotOccupied_PublishesSetTargetForActionWithStation2()
    {
        const string targetStation = "2";
   
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 1), new StationOccupancy(targetStation, 0), new StationOccupancy("3", 0)]);

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
            .WithOccupancies([new StationOccupancy("1", 1), new StationOccupancy("2", 1), new StationOccupancy(targetStation, 0)]);

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
            .WithOccupancies([new StationOccupancy(targetStation, 1), new StationOccupancy("2", 1), new StationOccupancy("3", 0)]);

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
            .WithOccupancies([new StationOccupancy("1", 2), new StationOccupancy(targetStation, 1), new StationOccupancy("3", 1)]);

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
        fixture.VerifyReportingToConveyors( times:Times.Never());
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
}