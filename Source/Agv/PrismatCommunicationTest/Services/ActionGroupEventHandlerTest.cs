using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
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
        var hostOrder = new HostOrder( "OrderId", "hu", DateTime.UtcNow);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());

        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        var actionGroupCompleted = new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();

        (await fixture.TestHarness.Published.Any<FinishHostOrder>(f =>
            f.Context.Message == new FinishHostOrder(actionGroup.CorrelationId, actionGroupCompleted.MessageCreatedDate, actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId))).ShouldBeTrue();
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
    public async Task PickActionCompleted_ValidData_PublishesFinishPick()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var pickActionCompleted = new PickActionCompleted(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        var finishPick = await fixture.PublishAndGetFirstMessageAsync<PickActionCompleted, FinishPick>(pickActionCompleted);

        finishPick.ActionCorrelationId.ShouldBe(pickActionCompleted.ActionCorrelationId);
        finishPick.CorrelationId.ShouldBe(pickActionCompleted.CorrelationId);
        finishPick.Timestamp.ShouldBe(pickActionCompleted.Timestamp);
    }

    [TestMethod]
    public async Task DroppingAgvPassedDoppelspielTriggerPoint_ValidData_PublishesPassTriggerPoint()
    {
        var agvPassedDoppelspielTriggerPoint = new DroppingAgvPassedDoppelspielTriggerPoint("drop target of action", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, "pickNode");

        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<PassTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupInvalid_RouteValidationError_PublishesCancelInvalidHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var orderEvent = new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, [HostOrderFailureReasons.ThereMustBeAtLeastOneAgvThatKnowsTheCompleteRouteOfAllActionsWithinTheGivenActionGroup]);

        await fixture.TestHarness.Bus.Publish(orderEvent);

        await fixture.ShouldHaveConsumedAsync<ActionGroupInvalid>();
        await fixture.ShouldHavePublishedAsync<CancelInvalidHostOrder>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupInvalid_NotRouteValidationError_PublishesCancelInvalidHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var orderEvent = new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, ["other validation error"]);

        await fixture.TestHarness.Bus.Publish(orderEvent);

        await fixture.ShouldHaveConsumedAsync<ActionGroupInvalid>();
        await fixture.ShouldNotHavePublishedAsync<CancelInvalidHostOrder>();
    }
}