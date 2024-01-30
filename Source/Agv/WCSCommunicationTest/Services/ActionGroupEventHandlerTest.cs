using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Models.HostOrder;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Commands;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services.EventHandler;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services;

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

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupInvalid>()).ShouldBeTrue();
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

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupStarted>()).ShouldBeTrue();
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

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupStarted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleHostOrderStart>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_CreatorIsTransportOrder_NoHandleHostOrderStart()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupStarted = new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupStarted);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupStarted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleHostOrderStart>()).ShouldBeFalse();
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

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCompleted>()).ShouldBeTrue();
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

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<FinishHostOrder>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_CreatorIsTransportOrder_NoFinishHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupCompleted = new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCompleted);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<FinishHostOrder>()).ShouldBeFalse();
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

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCanceled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<FinishHostOrderCancellation>(f =>
            f.Context.Message == new FinishHostOrderCancellation(actionGroup.CorrelationId, actionGroupCanceled.MessageCreatedDate))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_CreatorIsTransportOrderWebApi_NoFinishHostOrderCancellation()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupCanceled = new ActionGroupCanceled(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCanceled);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCanceled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<FinishHostOrderCancellation>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_CreatorIsTransportOrder_NoFinishHostOrderCancellation()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupCanceled = new ActionGroupCanceled(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCanceled);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCanceled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<FinishHostOrderCancellation>()).ShouldBeFalse();
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

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleCancellationFailure>(f =>
            f.Context.Message == new HandleCancellationFailure(actionGroup.CorrelationId, actionGroupCancelFailed.ErrorCode))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCancelFailed_CreatorIsTransportOrderWebApi_NoHandleCancellationFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupCancelFailed = new ActionGroupCancelFailed(actionGroup, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderDoesNotExist);

        await fixture.TestHarness.Bus.Publish(actionGroupCancelFailed);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleCancellationFailure>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCancelFailed_CreatorIsTransportOrder_NoHandleCancellationFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupCancelFailed = new ActionGroupCancelFailed(actionGroup, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderDoesNotExist);

        await fixture.TestHarness.Bus.Publish(actionGroupCancelFailed);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleHostOrderFailure>()).ShouldBeFalse();
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

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleHostOrderFailure>(f =>
            f.Context.Message == new HandleHostOrderFailure(actionGroup.CorrelationId, actionGroupFailed.ErrorCode, actionGroupFailed.MessageCreatedDate))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupFailed_CreatorIsTransportOrderWebApi_NoHandleHostOrderFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupFailed = new ActionGroupFailed(actionGroup, actionGroup.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupFailed);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleHostOrderFailure>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionGroupFailed_CreatorIsTransportOrder_NoHandleHostOrderFailure()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();

        var actionGroupFailed = new ActionGroupFailed(actionGroup, actionGroup.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupFailed);

        (await fixture.ConsumerHarness.Consumed.Any<ActionGroupFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HandleHostOrderFailure>()).ShouldBeFalse();
    }
}