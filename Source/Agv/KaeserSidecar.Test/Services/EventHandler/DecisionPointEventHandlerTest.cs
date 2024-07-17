using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class DecisionPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedDecisionPoint1_Station1NotOccupied_PublishesSetTargetForActionWithStation1()
    {
        const string targetStation = "1";
        var fixture = new DecisionPointEventHandlerTestFixture()
            .WithOccupancies([new StationOccupancy(targetStation, 0), new StationOccupancy("2", 0), new StationOccupancy("3", 0)]);

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
}