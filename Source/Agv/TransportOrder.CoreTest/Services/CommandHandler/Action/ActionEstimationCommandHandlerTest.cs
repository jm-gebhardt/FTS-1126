using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

[TestClass]
public class ActionEstimationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_OneAction_SetsEstimatedDuration()
    {
        var fixture = new ActionEstimationGroupTestFixture()
            .AddActionGroup(AgvActionGenerator.GenerateMoveActionGroup());

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), 1, fixture.Agv.CorrelationId, fixture.ActionGroup.CorrelationId));

        var updatedActionGroup = fixture.GetUpdatedActionGroup();
        updatedActionGroup.EstimatedDuration.ShouldNotBeNull();
        ((TimeSpan) updatedActionGroup.EstimatedDuration).TotalSeconds.ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_TwoActions_SetsEstimatedDuration()
    {
        var fixture = new ActionEstimationGroupTestFixture()
            .AddActionGroup(AgvActionGenerator.GeneratePickDropActionGroup())
            .AddRouteOnCosts(8);

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), 2, fixture.Agv.CorrelationId, fixture.ActionGroup.CorrelationId));

        var updatedActionGroup = fixture.GetUpdatedActionGroup();
        updatedActionGroup.EstimatedDuration.ShouldNotBeNull();
        // 2 (first action) + 8 (second action) + 2 (state transition)
        ((TimeSpan) updatedActionGroup.EstimatedDuration).TotalSeconds.ShouldBe(12);
    }

    [TestMethod]
    public async Task Consume_EstimateActionGroupDuration_ChargingAction_SetsEstimatedDuration()
    {
        var fixture = new ActionEstimationGroupTestFixture(45)
            .AddActionGroup(AgvActionGenerator.GenerateChargeActionGroup())
            .AddRouteOnCosts(0);

        await fixture.PublishMessageAsync(new EstimateActionGroupDuration(fixture.ActionGroup.Actions.First(), 3, fixture.Agv.CorrelationId, fixture.ActionGroup.CorrelationId));

        var updatedActionGroup = fixture.GetUpdatedActionGroup();
        updatedActionGroup.EstimatedDuration.ShouldNotBeNull();
        // 3 (move action) + (100-45)*1 (delta of 55 with 1% / s) + 2 (state transition)
        ((TimeSpan) updatedActionGroup.EstimatedDuration).TotalSeconds.ShouldBe(60);
    }
}