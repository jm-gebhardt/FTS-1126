using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.BackgroundServices;

[TestClass]
public class WaitingHandlerTest
{
    [TestMethod]
    public async Task ExecuteAsync_NoActionToHandle_DoesNotPublishSetTargetForAction()
    {
        var fixture = new WaitingHandlerTestFixture();

        await fixture.HandleAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
    }

    [TestMethod]
    public async Task ExecuteAsync_StationNotOccupied_PublishedSetTargetForAction()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        moveAction.StartedDate = DateTime.UtcNow;

        var fixture = new WaitingHandlerTestFixture()
            .WithOccupancy(new StationOccupancy(WaitingHandlerTestFixture.Target, 0, null))
            .AddActionGroup(AgvActionGenerator.ConvertFromMessage(actionGroup));

        await fixture.Handler.StartHandling(moveAction.CorrelationId, actionGroup.AssignedAgv!.Value, actionGroup.CreatedDate);
        await fixture.HandleAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.TargetNodeId.ShouldBe(WaitingHandlerTestFixture.Target);

        await fixture.VerifyStationOccupancyAsync(1, actionGroup.AssignedAgv!.Value);

        fixture.WaitingPointRepository.Verify(x => x.UpdateAsync(It.IsAny<WaitingPointAction>()), Times.Once);
        var waitingPointAction = fixture.WaitingPointRepository.Invocations.First().Arguments[0] as WaitingPointAction;
        waitingPointAction.ShouldNotBeNull();
        waitingPointAction.CorrelationId.ShouldBe(moveAction.CorrelationId);
        waitingPointAction.Target.ShouldBe(WaitingHandlerTestFixture.Target);
    }

    [TestMethod]
    public async Task ExecuteAsync_ActionInRepository_UsedForTargetSelection()
    {
        var agv = Guid.NewGuid();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        var actionGroup = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, agv);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        moveAction.StartedDate = DateTime.UtcNow;

        var fixture = new WaitingHandlerTestFixture()
            .WithOccupancy(new StationOccupancy(WaitingHandlerTestFixture.Target, 0, null))
            .AddActionGroup(AgvActionGenerator.ConvertFromMessage(actionGroup))
            .WithWaitingPointAction(new WaitingPointAction(moveAction.CorrelationId, agv, DateTime.Now));

        await fixture.HandleAsync();

        await fixture.ShouldHavePublishedAsync<SetTargetForAction>();
        var setTargetForAction = fixture.TestHarness.Published.Select<SetTargetForAction>().First().Context.Message;
        setTargetForAction.ShouldNotBeNull();
        setTargetForAction.ActionId.ShouldBe(moveAction.CorrelationId);
    }

    [TestMethod]
    public async Task ExecuteAsync_CompletedActionInRepository_NotUsedForTargetSelection()
    {
        var waitingPointAction = new WaitingPointAction(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now);
        waitingPointAction.SetTarget(WaitingHandlerTestFixture.Target);
        var fixture = new WaitingHandlerTestFixture()
            .WithOccupancy(new StationOccupancy(WaitingHandlerTestFixture.Target, 0, null))
            .WithWaitingPointAction(waitingPointAction);

        await fixture.HandleAsync();

        await fixture.ShouldNotHavePublishedAsync<SetTargetForAction>();
    }
}