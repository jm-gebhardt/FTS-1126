using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using MassTransit;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// OrderStateObserver Test
    /// </summary>
    [TestClass]
    public class OrderStateObserverTest
    {
        /// <summary>
        /// When OrderSaga state has changed check if observer was triggered
        /// </summary>
        [TestMethod]
        public async Task When_OrderSaga_State_Changed_Trigger_Observer()
        {
            var mockHub = new Mock<IOrderHubContext>();
            var mockContext = new Mock<BehaviorContext<OrderSaga>>();
            var mockLogger = new Mock<ILogger<OrderStateMachine>>();
            var mockAgvRegistry = new Mock<IAgvRegistry>();
            var orderStateObserver = new OrderStateObserver(mockHub.Object);
            var orderEventObserver = new Mock<IEventObserver<OrderSaga>>();
            var orderStateMachine = new OrderStateMachine(mockLogger.Object, mockAgvRegistry.Object, orderStateObserver, orderEventObserver.Object);
            var previousState = orderStateMachine.OrderReceived;
            var currentState = orderStateMachine.AgvRequested;

            await orderStateObserver.StateChanged(mockContext.Object, currentState, previousState);

            mockHub.Verify(s => s.SendOrderToAllClients("OrderStateChanged", It.IsAny<IOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
