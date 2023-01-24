using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Requests;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

[TestClass]
public class ActionManagerTest
{
    [TestMethod]
    public async Task AgvReady_Triggers_AssignAgvCommandOfPendingAction()
    {
        var correlationId = Guid.NewGuid();

        var fixture = new ActionManagerTestFixture()
            .PendingActionsRegistryMock(m =>
            {
                m.Setup(s => s.GetAllPendingActions()).Returns(new List<AgvAction>
                {
                    new(correlationId, ActionType.Transport, ActionManagerTestFixture.OrderDetails[0])
                });
            })
            .UseAssignAgvConsumer()
            .Build();

        await fixture.TestHarness!.Bus.Publish(new AgvReady(Guid.NewGuid()));
        (await fixture.TestHarness.Consumed.Any<AgvReady>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<AssignAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task AgvRequestsCharging_Triggers_AssignAgvCommandOfPendingAction()
    {
        var correlationId = Guid.NewGuid();

        var fixture = new ActionManagerTestFixture()
            .PendingActionsRegistryMock(m =>
            {
                m.Setup(s => s.GetAllPendingActions()).Returns(new List<AgvAction>
                {
                    new(correlationId, ActionType.Transport, ActionManagerTestFixture.OrderDetails[0])
                });
            })
            .UseAssignAgvConsumer()
            .Build();

        await fixture.TestHarness!.Bus.Publish(new AgvRequestsCharging(Guid.NewGuid()));
        (await fixture.TestHarness.Consumed.Any<AgvRequestsCharging>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task AgvNeedsCharging_Consumed()
    {
        var correlationId = Guid.NewGuid();

        var fixture = new ActionManagerTestFixture()
            .PendingActionsRegistryMock(m =>
            {
                m.Setup(s => s.GetAllPendingActions()).Returns(new List<AgvAction>
                {
                    new(correlationId, ActionType.Transport, ActionManagerTestFixture.OrderDetails[0])
                });
            })
            .UseAssignAgvConsumer()
            .Build();

        await fixture.TestHarness!.Bus.Publish(new AgvNeedsCharging(Guid.NewGuid()));
        (await fixture.TestHarness.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task RequestAssignmentAsync_Triggers_AssignAgvCommandOfAllPendingAction()
    {
        var fixture = new ActionManagerTestFixture()
            .PendingActionsRegistryMock(m =>
            {
                m.Setup(s => s.GetAllPendingActions()).Returns(new List<AgvAction>
                {
                    new(Guid.NewGuid(), ActionType.Transport, ActionManagerTestFixture.OrderDetails[0]),
                    new(Guid.NewGuid(), ActionType.Transport, ActionManagerTestFixture.OrderDetails[1]),
                    new(Guid.NewGuid(), ActionType.Transport, ActionManagerTestFixture.OrderDetails[2])
                });
            })
            .UseAssignAgvConsumer()
            .Build();

        await fixture.ActionManager.RequestAssignmentAsync(CancellationToken.None);

        (await fixture.TestHarness!.Published.Any<AssignAgv>(a => a.Context.Message.Order == ActionManagerTestFixture.OrderDetails[0])).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<AssignAgv>(a => a.Context.Message.Order == ActionManagerTestFixture.OrderDetails[1])).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<AssignAgv>(a => a.Context.Message.Order == ActionManagerTestFixture.OrderDetails[2])).ShouldBeTrue();
    }

    [TestMethod]
    public async Task OrderQueuingCommand_Triggers_CreateActionRequest()
    {
        var correlationId = Guid.NewGuid();
        var orderDetails = ActionManagerTestFixture.OrderDetails[0];
        var request = new OrderQueuingCommand(correlationId, orderDetails);

        var fixture = new ActionManagerTestFixture()
            .SetActionCreationSucceededEventResponse(new ActionCreationSucceededEvent(correlationId))
            .Build();

        await fixture.TestHarness!.Bus.Publish(request);
        (await fixture.TestHarness.Consumed.Any<OrderQueuingCommand>()).ShouldBeTrue();

        (await fixture.TestHarness!.Published.Any<CreateActionRequest>()).ShouldBeTrue();
        var sentRequest = fixture.TestHarness.Published.Select<CreateActionRequest>().First().Context.Message;
        sentRequest.Action.OrderDetails.ShouldBe(orderDetails);
        sentRequest.Action.ActionType.ShouldBe(ActionType.Transport);

        var sentOrderQueuedEvent = fixture.TestHarness.Published.Select<OrderQueuedEvent>().First().Context.Message;
        sentOrderQueuedEvent.CorrelationId.ShouldBe(correlationId);
    }

    [TestMethod]
    public async Task ActionAssignedEvent_IsPublished_When_AgvAssignmentSucceeded()
    {
        var correlationId = Guid.NewGuid();
        var testFixture = new ActionManagerTestFixture();

        var agv = testFixture.Agvs.First();
        string agvId = agv.AgvId;
        var assignedAgvCorrelationId = agv.CorrelationId;
        var agvAction = new AgvAction(correlationId, ActionType.Transport, ActionManagerTestFixture.OrderDetails[0]);

        testFixture.PendingActionsRegistryMock(m =>
            {
                m.Setup(s => s.GetAllPendingActions()).Returns(new List<AgvAction>
                {
                    agvAction
                });
            })
            .SetAgvAssignmentResponse(new AgvAssignmentSucceeded
            (
                correlationId,
                agvId,
                assignedAgvCorrelationId,
                Array.Empty<string>()
            ))
        .Build();

        // create ActionStateMachine
        await testFixture.TestHarness!.Bus.Publish(new CreateActionRequest(agvAction), CancellationToken.None);
        (await testFixture.TestHarness.Consumed.Any<CreateActionRequest>()).ShouldBeTrue();

        // assign agv to action
        await testFixture.ActionManager.RequestAssignmentAsync(CancellationToken.None);
        (await testFixture.TestHarness!.Published.Any<AssignActionRequest>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task When_AgvAssignmentFailed_NoRouteFound_HostOrderHandlingFailedPublished()
    {
        var correlationId = Guid.NewGuid();
        var testFixture = new ActionManagerTestFixture();
        var orderDetails = ActionManagerTestFixture.OrderDetails[0];
        var orderCorrelationId = orderDetails.CorrelationId;
        testFixture.PendingActionsRegistryMock(m =>
            {
                m.Setup(s => s.GetAllPendingActions()).Returns(new List<AgvAction>
                {
                    new(correlationId, ActionType.Transport, orderDetails)
                });
            })
            .SetAgvAssignmentResponse(new AgvAssignmentFailed
            (
                correlationId,
                AgvAssignmentFailed.ErrorCode.NoRouteFound
            ))
            .Build();

        await testFixture.ActionManager.RequestAssignmentAsync(CancellationToken.None);
        (await testFixture.TestHarness!.Published.Any<HostOrderHandlingFailed>()).ShouldBeTrue();
        var message = testFixture.TestHarness.Published.Select<HostOrderHandlingFailed>().First().Context.Message;
        message.CorrelationId.ShouldBe(orderCorrelationId);
        message.Error.ShouldBe(HostOrderHandlingFailed.ErrorCode.InvalidOrderData);
    }

}