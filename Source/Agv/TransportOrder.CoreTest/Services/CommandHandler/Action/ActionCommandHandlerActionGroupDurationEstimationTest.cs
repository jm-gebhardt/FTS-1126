using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

[TestClass]
public class ActionCommandHandlerActionGroupDurationEstimationTest
{
    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_NotFirstAction_NotUpdatedPublishesActionDurationEstimated()
    {
        var fixture = new ActionEstimationGroupTestFixture()
            .AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup(duration: TimeSpan.FromMinutes(2)));

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.Last(), fixture.Agv.CorrelationId, fixture.ActionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionDurationEstimated>()).ShouldBeTrue();
        fixture.VerifyActionGroupUpdate(Times.Never());
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_AgvMismatch_NotUpdatedDoesNotPublishActionDurationEstimated()
    {
        var fixture = new ActionEstimationGroupTestFixture()
            .AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup());

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), "another agv".ToGuid(), fixture.ActionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionDurationEstimated>()).ShouldBeFalse();
        fixture.VerifyActionGroupUpdate(Times.Never());
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_ValidData_PublishActionDurationEstimatedUpdateActionGroup()
    {
        var fixture = new ActionEstimationGroupTestFixture();
            fixture.AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv:fixture.Agv.CorrelationId, duration: TimeSpan.Zero))
            .AddRouteOnCosts(42);

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), fixture.ActionGroup.AssignedAgv!.Value, fixture.ActionGroup.CorrelationId));

        (await fixture.TestHarness.Published.Any<ActionDurationEstimated>()).ShouldBeTrue();
        var actionGroup = fixture.VerifyActionGroupUpdate(Times.Once());
        actionGroup.EstimatedDuration.ShouldNotBe(TimeSpan.Zero);
    }
}