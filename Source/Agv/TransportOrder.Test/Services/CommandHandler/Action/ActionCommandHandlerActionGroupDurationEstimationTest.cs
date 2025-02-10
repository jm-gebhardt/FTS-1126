using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

[TestClass]
public class ActionCommandHandlerActionGroupDurationEstimationTest
{
    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_NotFirstAction_NotUpdatedPublishesActionDurationEstimated()
    {
        var fixture = new ActionGroupEstimationTestFixture()
            .WithLastActionIsMoveToCoordinate(false)
            .AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup(duration: TimeSpan.FromMinutes(2)));

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.Last(), fixture.Agv.CorrelationId, fixture.ActionGroup.CorrelationId));
        
        await fixture.ShouldHavePublishedAsync<ActionDurationEstimated>();
        fixture.VerifyActionGroupUpdate(Times.Never());
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_AgvMismatch_NotUpdatedDoesNotPublishActionDurationEstimated()
    {
        var fixture = new ActionGroupEstimationTestFixture()
            .WithLastActionIsMoveToCoordinate(false)
            .AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup());

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), "another agv".ToGuid(), fixture.ActionGroup.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ActionDurationEstimated>();
        fixture.VerifyActionGroupUpdate(Times.Never());
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_ValidData_PublishActionDurationEstimatedUpdateActionGroup()
    {
        var fixture = new ActionGroupEstimationTestFixture()
            .WithLastActionIsMoveToCoordinate(false);
        fixture.AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: fixture.Agv.CorrelationId, duration: TimeSpan.Zero))
            .AddRouteOnCosts(42).AddNonRouteCosts(1);

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), fixture.ActionGroup.AssignedAgv!.Value, fixture.ActionGroup.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ActionDurationEstimated>();
        var actionGroup = fixture.VerifyActionGroupUpdate(Times.Once());
        actionGroup.EstimatedDuration.ShouldNotBe(TimeSpan.Zero);
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_MoveToCoordinate_PublishActionDurationEstimatedUpdateActionGroup()
    {
        var fixture = new ActionGroupEstimationTestFixture()
            .WithLastActionIsMoveToCoordinate(false)
            .AddDuration(2);
        fixture.AddActionGroup(AgvActionGenerator.GenerateMoveToCoordinateActionGroup(assignedAgv: fixture.Agv.CorrelationId, duration: TimeSpan.Zero));

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), fixture.ActionGroup.AssignedAgv!.Value, fixture.ActionGroup.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ActionDurationEstimated>();
        var actionGroup = fixture.VerifyActionGroupUpdate(Times.Once());
        actionGroup.EstimatedDuration.ShouldNotBe(TimeSpan.Zero);
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_MoveFromCoordinateToGraph_PublishActionDurationEstimatedUpdateActionGroup()
    {
        var fixture = new ActionGroupEstimationTestFixture()
            .WithLastActionIsMoveToCoordinate(true)
            .AddDuration(2);
        fixture.AddActionGroup(AgvActionGenerator.GenerateMoveActionGroup(assignedAgv: fixture.Agv.CorrelationId, duration: TimeSpan.Zero));

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), fixture.ActionGroup.AssignedAgv!.Value, fixture.ActionGroup.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ActionDurationEstimated>();
        var actionGroup = fixture.VerifyActionGroupUpdate(Times.Once());
        actionGroup.EstimatedDuration.ShouldNotBe(TimeSpan.Zero);
    }
}