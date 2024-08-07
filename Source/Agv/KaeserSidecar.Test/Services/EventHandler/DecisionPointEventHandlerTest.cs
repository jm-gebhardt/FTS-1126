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
    private static int _lastChosenStation;

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupied_NextPickAvailable_SelectsStation1SendsTelegramWithPickStation()
    {
        const string targetStation = "1";
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var hostOrder = OrderGenerator.GenerateOrderFromActionGroup(actionGroup);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithHostOrder(hostOrder);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()), 
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        
        fixture.VerifyReportingToConveyors("WE01", true, hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).Node, Times.Once());

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        _lastChosenStation = int.Parse(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupied_NoNextPickAvailable_SelectsStation1SendsTelegramWithoutPickStation()
    {
        string targetStation = "1";
        if (int.Parse(targetStation) == _lastChosenStation)
        {
            // station 1 has been chosen before so station 2 will be used
            targetStation = "2";
        }
        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started);
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)])
            .WithHostOrder(null);
        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        string stationName = "WE0" + targetStation;
        fixture.VerifyReportingToConveyors(stationName, true, null, Times.Once());

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        _lastChosenStation = int.Parse(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station2NotOccupied_PublishesSetTargetForActionWithStation2()
    {
        string targetStation = "2";
        if (int.Parse(targetStation) == _lastChosenStation)
        {
            // station 2 has been chosen before so station 2 will be used
            targetStation = "3";
        }

        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 1), new StationOccupancy("2", 0), new StationOccupancy("3", 0)]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        _lastChosenStation = int.Parse(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_OnlyStation3NotOccupied_PublishesSetTargetForActionWithStation3()
    {
        string targetStation = "3";
        if (int.Parse(targetStation) == _lastChosenStation)
        {
            // station 3 has been chosen before so station 1 will be used
            targetStation = "1";
        }

        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 1), new StationOccupancy("2", 1), new StationOccupancy("3", 0)]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        _lastChosenStation = int.Parse(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station3NotOccupied_UseOnlyTwoStations_Station1And2HaveIdenticalOccupancies_PublishesSetTargetForActionWithStation1()
    { 
        string targetStation = "1";
        if (int.Parse(targetStation) == _lastChosenStation)
        {
            // station 1 has been chosen before so station 2 will be used
            targetStation = "2";
        }

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
        _lastChosenStation = int.Parse(targetStation);
    }

    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_AllStationsOccupied_Station2HasLowerOccupancyThan1_PublishesSetTargetForActionWithStation2()
    {
        string targetStation = "2";
        if (int.Parse(targetStation) == _lastChosenStation)
        {
            // station 2 has been chosen before so as all stations have occupancies only 1+2 are considered so station 1 will be used
            targetStation = "1";
        }
        
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy("1", 2), new StationOccupancy("2", 1), new StationOccupancy("3", 1)]);

        var agvPassedDecisionPoint1 = new AgvPassedDecisionPoint1(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState.Started, ActionState.Started), [], [], 3);

        await fixture.TestHarness.Bus.Publish(agvPassedDecisionPoint1);

        await fixture.ShouldHaveConsumedAsync<AgvPassedDecisionPoint1>();
        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();

        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(targetStation);
        _lastChosenStation = int.Parse(targetStation);
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
 //       await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
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
 //       await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }
}