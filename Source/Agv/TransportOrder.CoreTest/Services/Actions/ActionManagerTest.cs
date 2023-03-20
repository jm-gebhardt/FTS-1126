using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions
{
    [TestClass]
    public class ActionManagerTest
    {
        /// <summary />
        public ActionManagerTest()
        {
            var fixture = new ActionManagerTestFixture().Build();
            // Initialize EntityFramework
            fixture.CreateOrderActionRegistry().SaveChanges();
        }

        [TestMethod]
        public async Task OrderQueuingCommand_Triggers_ActionsCreated()
        {
            // Order being queued
            var orderDetails = ActionManagerTestFixture.OrderDetails[0];

            var fixture = new ActionManagerTestFixture()
                .SetAgvAssignmentResponse(new AgvAssignmentFailed(Guid.NewGuid(), AgvAssignmentFailed.ErrorCode.NoRouteFound))
                .Build();

            await fixture.TestHarness!.Bus.Publish(new OrderQueuingCommand(Guid.NewGuid(), orderDetails));
            (await fixture.TestHarness.Consumed.Any<OrderQueuingCommand>()).ShouldBeTrue();

            (await fixture.TestHarness.Published.Any<OrderQueuedEvent>()).ShouldBeTrue();

            // Called due to reason "NoRouteFound"
            (await fixture.TestHarness.Published.Any<HostOrderHandlingFailed>()).ShouldBeTrue();

            // Actions still there in status "Created"
            using var orderActionRepo = fixture.CreateOrderActionRegistry();
            orderActionRepo.GetActionsOfOrder(orderDetails.CorrelationId).Count.ShouldBe(2);
            List<OrderAction> actions = orderActionRepo.GetActionsOfOrder(orderDetails.CorrelationId);
            foreach (var action in actions)
            {
                action.State.ShouldBe(OrderActionState.Created);
            }
            actions[0].Type.ShouldBe(ActionType.Pick);
            actions[0].Source.ShouldBeNull();
            actions[0].Destination.ShouldBe(orderDetails.Source);
            actions[1].Type.ShouldBe(ActionType.Drop);
            actions[1].Source.ShouldBe(orderDetails.Source);
            actions[1].Destination.ShouldBe(orderDetails.Destination);
        }

        [TestMethod]
        public async Task AgvBecameReady_Triggers_AssignAgvForPendingAction()
        {
            var orderDetails = ActionManagerTestFixture.OrderDetails[0];

            var fixture = new ActionManagerTestFixture();
            var agv = fixture.Agvs.First();
            string agvId = agv.AgvId;
            var assignedAgvCorrelationId = agv.CorrelationId;
            string[] nodes = new string[]
            {
                orderDetails.Source,
                orderDetails.Destination
            };

            fixture
                .SetAgvAssignmentResponse(new AgvAssignmentSucceeded(Guid.NewGuid(), agvId, assignedAgvCorrelationId, nodes))
                .Build();

            using var inserter = fixture.CreateOrderActionRegistry();
            inserter.Insert(orderDetails, ActionType.Pick, DateTime.UtcNow, null, orderDetails.Source);
            inserter.Insert(orderDetails, ActionType.Drop, DateTime.UtcNow, orderDetails.Source, orderDetails.Destination);
            inserter.SaveChanges();

            await fixture.TestHarness!.Bus.Publish(new AgvBecameReady(Guid.NewGuid()));
            (await fixture.TestHarness.Consumed.Any<AgvBecameReady>()).ShouldBeTrue();

            (await fixture.TestHarness.Published.Any<OrderAssignmentSucceeded>()).ShouldBeTrue();
            // Check content of published message
            var orderMsg = fixture.TestHarness.Published.Select<OrderAssignmentSucceeded>().First().Context.Message;
            orderMsg.CorrelationId.ShouldBe(orderDetails.CorrelationId);
            orderMsg.AgvId.ShouldBe(agvId);
            orderMsg.AgvCorrelationId.ShouldBe(assignedAgvCorrelationId);

            (await fixture.TestHarness.Published.Any<StartActionCommand>()).ShouldBeTrue();
            // Check content of published message
            var startMsg = fixture.TestHarness.Published.Select<StartActionCommand>().First().Context.Message;
            startMsg.CorrelationId.ShouldBe(assignedAgvCorrelationId);
            startMsg.Action.AgvId.ShouldBe(agvId);

            (await fixture.TestHarness.Published.Any<ActionStartedEvent>()).ShouldBeTrue();
            // Check content of published message
            var actionMsg = fixture.TestHarness.Published.Select<ActionStartedEvent>().First().Context.Message;
            actionMsg.CorrelationId.ShouldBe(assignedAgvCorrelationId);

            using var getter = fixture.CreateOrderActionRegistry();
            List<OrderAction> actions = getter.GetActionsOfOrder(orderDetails.CorrelationId);
            actions[0].State.ShouldBe(OrderActionState.Started);
            actions[0].Type.ShouldBe(ActionType.Pick);
            actions[0].Source.ShouldBeNull();
            actions[0].Destination.ShouldBe(orderDetails.Source);
            actions[1].State.ShouldBe(OrderActionState.Assigned);
            actions[1].Type.ShouldBe(ActionType.Drop);
            actions[1].Source.ShouldBe(orderDetails.Source);
            actions[1].Destination.ShouldBe(orderDetails.Destination);
        }

        [TestMethod]
        public async Task ActionStatusChangedEvent_Triggers_ActionProgress()
        {
            var orderDetails = ActionManagerTestFixture.OrderDetails[0];

            var fixture = new ActionManagerTestFixture()
                .Build();

            using var inserter = fixture.CreateOrderActionRegistry();
            inserter.Insert(orderDetails, ActionType.Pick, DateTime.UtcNow, null, orderDetails.Source);
            inserter.SaveChanges();
            var actionId = inserter.GetActionsOfOrder(orderDetails.CorrelationId)[0].Id;

            await fixture.TestHarness!.Bus.Publish(new ActionStartedEvent(actionId, string.Empty));
            (await fixture.TestHarness.Published.Any<TransportOrderStarted>()).ShouldBeTrue();

            await fixture.TestHarness!.Bus.Publish(new ActionCancelledEvent(actionId, string.Empty));
            (await fixture.TestHarness.Published.Any<TransportOrderCancelSucceeded>()).ShouldBeTrue();

            await fixture.TestHarness!.Bus.Publish(new ActionFailedEvent(actionId, string.Empty));
            (await fixture.TestHarness.Published.Any<TransportOrderFailed>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task ActionCompleted_DeleteAllActions_WhenNoSubsequentActions()
        {
            var order1 = ActionManagerTestFixture.OrderDetails[0];
            var order2 = ActionManagerTestFixture.OrderDetails[1];

            var fixture = new ActionManagerTestFixture();
            var agv = fixture.Agvs.First();
            string agvId = agv.AgvId;
            var assignedAgvCorrelationId = agv.CorrelationId;
            string[] nodes = new string[]
            {
                order1.Source,
                order1.Destination,
                order2.Source,
                order2.Destination
            };
            fixture
                .SetAgvAssignmentResponse(new AgvAssignmentSucceeded(Guid.NewGuid(), agvId, assignedAgvCorrelationId, nodes))
                .Build();

            using var inserter = fixture.CreateOrderActionRegistry();
            // Insert action of first order
            inserter.Insert(order1, ActionType.Move, DateTime.UtcNow, null, order1.Destination);
            inserter.SaveChanges();
            // Ensure different creation timestamp
            Thread.Sleep(10);
            // Insert action of second order
            inserter.Insert(order2, ActionType.Charge, DateTime.UtcNow, null, order2.Destination);
            inserter.SaveChanges();

            // Assign action of first order
            await fixture.TestHarness!.Bus.Publish(new AgvBecameReady(Guid.NewGuid()));

            (await fixture.TestHarness.Consumed.Any<AgvBecameReady>()).ShouldBeTrue();

            // Complete action of first order
            var actionId1 = inserter.GetActionsOfOrder(order1.CorrelationId)[0].Id;
            await fixture.TestHarness!.Bus.Publish(new ActionCompletedEvent(actionId1, string.Empty));
            (await fixture.TestHarness.Consumed.Any<ActionCompletedEvent>()).ShouldBeTrue();

            // Expect ReleaseAgv due to action completed ...
            (await fixture.TestHarness.Published.Any<ReleaseAgv>()).ShouldBeTrue();
            // ... and therefore order is completed, too.
            (await fixture.TestHarness.Published.Any<TransportOrderCompleted>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task ActionCompleted_Triggers_NextAction()
        {
            var order1 = ActionManagerTestFixture.OrderDetails[0];

            var fixture = new ActionManagerTestFixture();
            var agv = fixture.Agvs.First();
            string agvId = agv.AgvId;
            var assignedAgvCorrelationId = agv.CorrelationId;
            string[] nodes = new string[]
            {
                order1.Source,
                order1.Destination
            };
            fixture
                .SetAgvAssignmentResponse(new AgvAssignmentSucceeded(Guid.NewGuid(), agvId, assignedAgvCorrelationId, nodes))
                .Build();

            using var inserter = fixture.CreateOrderActionRegistry();
            // Insert action of first order
            inserter.Insert(order1, ActionType.Move, DateTime.UtcNow, null, order1.Destination);
            inserter.Insert(order1, ActionType.Move, DateTime.UtcNow, null, order1.Destination);
            inserter.SaveChanges();

            // Assign action of first order ...
            await fixture.TestHarness!.Bus.Publish(new AgvBecameReady(Guid.NewGuid()));
            (await fixture.TestHarness.Consumed.Any<AgvBecameReady>()).ShouldBeTrue();
            // ... what will start this action
            (await fixture.TestHarness.Published.Any<StartActionCommand>()).ShouldBeTrue();

            // Complete first action of first order ...
            var actionId1 = inserter.GetActionsOfOrder(order1.CorrelationId)[0].Id;
            await fixture.TestHarness!.Bus.Publish(new ActionCompletedEvent(actionId1, string.Empty));
            (await fixture.TestHarness.Consumed.Any<ActionCompletedEvent>()).ShouldBeTrue();
            // ... and start subsequent action
            fixture.TestHarness.Published.Select<StartActionCommand>().Count().ShouldBe(2);

            // Expect first action being completed
            using var getter = fixture.CreateOrderActionRegistry();
            var action = getter.GetActionById(actionId1);
            action!.State.ShouldBe(OrderActionState.Completed);
            action.OrderCorrelationId.ShouldBe(order1.CorrelationId);

            // Expect subsequent action being started
            action = getter.GetActionsOfOrder(order1.CorrelationId)[1];
            action.State.ShouldBe(OrderActionState.Started);
            action.OrderCorrelationId.ShouldBe(order1.CorrelationId);
        }
    }
}