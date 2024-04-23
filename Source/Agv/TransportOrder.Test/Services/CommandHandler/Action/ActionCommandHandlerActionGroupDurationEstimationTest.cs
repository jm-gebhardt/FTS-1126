using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
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
            .AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup(duration: TimeSpan.FromMinutes(2)));

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.Last(), fixture.Agv.CorrelationId, fixture.ActionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionDurationEstimated>()).ShouldBeTrue();
        fixture.VerifyActionGroupUpdate(Times.Never());
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_AgvMismatch_NotUpdatedDoesNotPublishActionDurationEstimated()
    {
        var fixture = new ActionGroupEstimationTestFixture()
            .AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup());

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), "another agv".ToGuid(), fixture.ActionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionDurationEstimated>()).ShouldBeFalse();
        fixture.VerifyActionGroupUpdate(Times.Never());
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_ValidData_PublishActionDurationEstimatedUpdateActionGroup()
    {
        var fixture = new ActionGroupEstimationTestFixture();
        fixture.AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: fixture.Agv.CorrelationId, duration: TimeSpan.Zero))
            .AddRouteOnCosts(42).AddDurationEstimateDeviation();

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), fixture.ActionGroup.AssignedAgv!.Value, fixture.ActionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionDurationEstimated>()).ShouldBeTrue();
        var actionGroup = fixture.VerifyActionGroupUpdate(Times.Once());
        actionGroup.EstimatedDuration.ShouldNotBe(TimeSpan.Zero);
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_PickDropActionGroup_ConsidersDeviation()
    {
        const int deviation = 10;
        var fixture = new ActionGroupEstimationTestFixture();
        fixture.AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: fixture.Agv.CorrelationId, duration: TimeSpan.Zero))
            .AddRouteOnCosts(0).AddDurationEstimateDeviation(deviation);

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), fixture.ActionGroup.AssignedAgv!.Value, fixture.ActionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionDurationEstimated>()).ShouldBeTrue();
        var actionGroup = fixture.VerifyActionGroupUpdate(Times.Once());
        actionGroup.EstimatedDuration.ShouldBe(TimeSpan.FromSeconds(deviation + actionGroup.Actions.Count * ActionCosts.SwitchingActions));
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_NotPickDropActionGroup_DoesNotConsidersDeviation()
    {
        const int deviation = 5;
        const int routeCost = 10;
        var fixture = new ActionGroupEstimationTestFixture();
        fixture.AddActionGroup(AgvActionGenerator.GenerateMoveActionGroup(assignedAgv: fixture.Agv.CorrelationId, duration: TimeSpan.Zero))
            .AddRouteOnCosts(routeCost).AddDurationEstimateDeviation(deviation);

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), fixture.ActionGroup.AssignedAgv!.Value, fixture.ActionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionDurationEstimated>()).ShouldBeTrue();
        var actionGroup = fixture.VerifyActionGroupUpdate(Times.Once());
        actionGroup.EstimatedDuration.ShouldBe(TimeSpan.FromSeconds(routeCost + actionGroup.Actions.Count * ActionCosts.SwitchingActions));
    }
}