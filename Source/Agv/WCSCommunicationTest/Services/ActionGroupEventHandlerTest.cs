using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.WCSCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.WCSCommunication.Messages.Commands;
using Gebhardt.Acc.WCSCommunication.Services.EventHandler;
using Gebhardt.Acc.WCSCommunicationTest.Fixtures;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.WCSCommunicationTest.Services;

[TestClass]
public class ActionGroupEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionGroupInvalid_ValidData_RemoveInvalidHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var actionGroupInvalid = new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, new List<string> { "a validation error" });

        await fixture.TestHarness.Bus.Publish(actionGroupInvalid);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupInvalid>();
        (await fixture.TestHarness.Published.Any<RemoveInvalidHostOrder>(f =>
            f.Context.Message == new RemoveInvalidHostOrder(actionGroupInvalid.CorrelationId))).ShouldBeTrue();
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
            f.Context.Message == new HandleHostOrderStart(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value.ToString(), actionGroupStarted.MessageCreatedDate))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_CreatorIsTransportOrderWebApi_NoHandleHostOrderStart()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupStarted = new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupStarted);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<HandleHostOrderStart>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_CreatorIsTransportOrder_NoHandleHostOrderStart()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupStarted = new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupStarted);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        await fixture.ShouldNotHavePublishedAsync<HandleHostOrderStart>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_ValidData_FinishHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var hostOrder = new HostOrder(actionGroup.CorrelationId, "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        var actionGroupCompleted = new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCompleted);
        
        (await fixture.TestHarness.Published.Any<FinishHostOrder>(f =>
            f.Context.Message == new FinishHostOrder(actionGroup.CorrelationId, actionGroupCompleted.MessageCreatedDate))).ShouldBeTrue();
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
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
    public async Task Consume_ActionGroupCompleted_CreatorIsTransportOrder_NoFinishHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupCompleted = new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCompleted);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
        await fixture.ShouldNotHavePublishedAsync<FinishHostOrder>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_ValidData_FinishHostOrderCancellation()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var hostOrder = new HostOrder(actionGroup.CorrelationId, "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        var actionGroupCanceled = new ActionGroupCanceled(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCanceled);
        
        (await fixture.TestHarness.Published.Any<FinishHostOrderCancellation>(f =>
            f.Context.Message == new FinishHostOrderCancellation(actionGroup.CorrelationId, actionGroupCanceled.MessageCreatedDate))).ShouldBeTrue();
        await fixture.ShouldHaveConsumedAsync<ActionGroupCanceled>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_CreatorIsTransportOrderWebApi_NoFinishHostOrderCancellation()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupCanceled = new ActionGroupCanceled(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCanceled);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupCanceled>();
        await fixture.ShouldNotHavePublishedAsync<FinishHostOrderCancellation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_CreatorIsTransportOrder_NoFinishHostOrderCancellation()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupCanceled = new ActionGroupCanceled(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCanceled);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupCanceled>();
        await fixture.ShouldNotHavePublishedAsync<FinishHostOrderCancellation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCancelFailed_ValidData_HandleCancellationFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var hostOrder = new HostOrder(actionGroup.CorrelationId, "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        var actionGroupCancelFailed = new ActionGroupCancelFailed(actionGroup, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderDoesNotExist);

        await fixture.TestHarness.Bus.Publish(actionGroupCancelFailed);

        (await fixture.TestHarness.Published.Any<HandleCancellationFailure>(f =>
            f.Context.Message == new HandleCancellationFailure(actionGroup.CorrelationId, actionGroupCancelFailed.ErrorCode))).ShouldBeTrue();
        await fixture.ShouldHaveConsumedAsync<ActionGroupCancelFailed>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCancelFailed_CreatorIsTransportOrderWebApi_NoHandleCancellationFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupCancelFailed = new ActionGroupCancelFailed(actionGroup, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderDoesNotExist);

        await fixture.TestHarness.Bus.Publish(actionGroupCancelFailed);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupCancelFailed>();
        await fixture.ShouldNotHavePublishedAsync<HandleCancellationFailure>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCancelFailed_CreatorIsTransportOrder_NoHandleCancellationFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupCancelFailed = new ActionGroupCancelFailed(actionGroup, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderDoesNotExist);

        await fixture.TestHarness.Bus.Publish(actionGroupCancelFailed);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupCancelFailed>();
        await fixture.ShouldNotHavePublishedAsync<HandleHostOrderFailure>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupFailed_ValidData_HandleHostOrderFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var hostOrder = new HostOrder(actionGroup.CorrelationId, "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        var actionGroupFailed = new ActionGroupFailed(actionGroup, actionGroup.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupFailed);
        await fixture.ShouldHaveConsumedAsync<ActionGroupFailed>();
        (await fixture.TestHarness.Published.Any<HandleHostOrderFailure>(f =>
            f.Context.Message == new HandleHostOrderFailure(actionGroup.CorrelationId, actionGroupFailed.ErrorCode, actionGroupFailed.MessageCreatedDate))).ShouldBeTrue();
        await fixture.ShouldNotHaveConsumedAsync<ActionGroupCancelFailed>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupFailed_CreatorIsTransportOrderWebApi_NoHandleHostOrderFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupFailed = new ActionGroupFailed(actionGroup, actionGroup.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupFailed);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupFailed>();
        await fixture.ShouldNotHavePublishedAsync<HandleHostOrderFailure>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupFailed_CreatorIsTransportOrder_NoHandleHostOrderFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupFailed = new ActionGroupFailed(actionGroup, actionGroup.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupFailed);
        
        await fixture.ShouldHaveConsumedAsync<ActionGroupFailed>();
        await fixture.ShouldNotHavePublishedAsync<HandleHostOrderFailure>();
    }
}