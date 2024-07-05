using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class ActionGroupEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionGroupCompleted_ValidData_FinishHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var hostOrder = new HostOrder(actionGroup.CorrelationId, "OrderId", "hu", DateTime.UtcNow);
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        var actionGroupCompleted = new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();

        (await fixture.TestHarness.Published.Any<FinishHostOrder>(f =>
            f.Context.Message == new FinishHostOrder(actionGroup.CorrelationId, actionGroupCompleted.MessageCreatedDate))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_CreatorIsTransportOrderWebApi_NoFinishHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupCompleted = new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCompleted);

        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
        await fixture.ShouldNotHavePublishedAsync<FinishHostOrder>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_ValidData_HandleHostOrderStart()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());

        var actionGroupStarted = new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupStarted);

        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        (await fixture.TestHarness.Published.Any<HandleHostOrderStart>(f =>
            f.Context.Message == new HandleHostOrderStart(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value, actionGroupStarted.MessageCreatedDate))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SetTargetForAction_ValidData_PublishesPassDecisionPoint()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();

        var setTargetForAction = new SetTargetForAction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "a target node");

        var passDecisionPoint = await fixture.PublishAndGetFirstMessageAsync<SetTargetForAction, PassDecisionPoint>(setTargetForAction);

        passDecisionPoint.AssignedAgv.ShouldBe(setTargetForAction.AssignedAgv);
        passDecisionPoint.TargetNodeId.ShouldBe(setTargetForAction.TargetNodeId);
        passDecisionPoint.CorrelationId.ShouldBe(setTargetForAction.ActionGroupId);
    }

    [TestMethod]
    public async Task AgvPassedLoadTriggerPoint_ValidData_PublishesPassLoadTriggerPoint()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();

        var agvPassedLoadTriggerPoint = new AgvPassedLoadTriggerPoint(Guid.NewGuid(), Guid.NewGuid());

        var passLoadTriggerPoint = await fixture.PublishAndGetFirstMessageAsync<AgvPassedLoadTriggerPoint, PassLoadTriggerPoint>(agvPassedLoadTriggerPoint);

        passLoadTriggerPoint.CorrelationId.ShouldBe(agvPassedLoadTriggerPoint.ActionGroupCorrelationId.GetValueOrDefault());
        passLoadTriggerPoint.AgvCorrelationId.ShouldBe(agvPassedLoadTriggerPoint.CorrelationId);
    }

    [TestMethod]
    public async Task PickActionCompleted_ValidData_PublishesFinishPick()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var pickActionCompleted = new PickActionCompleted(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        var finishPick = await fixture.PublishAndGetFirstMessageAsync<PickActionCompleted, FinishPick>(pickActionCompleted);

        finishPick.ActionCorrelationId.ShouldBe(pickActionCompleted.ActionCorrelationId);
        finishPick.CorrelationId.ShouldBe(pickActionCompleted.CorrelationId);
        finishPick.Timestamp.ShouldBe(pickActionCompleted.Timestamp);
    }
}