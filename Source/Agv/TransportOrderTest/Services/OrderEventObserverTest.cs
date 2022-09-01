using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Class dedicated to testing OrderEventObserver
    /// </summary>
    [TestClass]
    public class OrderEventObserverTest
    {
        /// <summary>
        /// Checks if IOrderAuditRegistry's Add() method is invoked as soon as any PreHook or PostHook is called
        /// </summary>
        [DataRow(Hook.PreHook)]
        [DataRow(Hook.PostHook)]
        [DataRow(Hook.ExecuteFault)]
        [TestMethod]
        public async Task AddMethod_Invocation_of_OrderEventObserver(Hook hook)
        {
            // Arrange
            string orderId = "Order123";
            var orderSaga = new OrderSaga(
                orderId.ToGuid(),
                new OrderDetails(orderId.ToGuid(), orderId, "S1", "D1", "1234", HostOrderCreator.Simulation),
                "Initial",
                null
            );

            var orderAuditRegistryMock = new Mock<IOrderAuditRegistry>();
            orderAuditRegistryMock.Setup(s => s.Add(orderSaga, It.IsAny<string>(), It.IsAny<string>(), hook, It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            
            var loggerMock = new Mock<ILogger<OrderEventObserver>>();
            var orderEventObserver = new OrderEventObserver(loggerMock.Object, orderAuditRegistryMock.Object);
            
            var bhContextGeneric = new Mock<BehaviorContext<OrderSaga, It.IsAnyType>>();
            bhContextGeneric.SetupGet(s => s.CorrelationId).Returns(orderSaga.CorrelationId);
            bhContextGeneric.SetupGet(s => s.Event.Name).Returns(It.IsAny<string>());
            bhContextGeneric.Setup(s => s.Saga).Returns(orderSaga);

            // Act
            await orderEventObserver.PreExecute(bhContextGeneric.Object);
            await orderEventObserver.PostExecute(bhContextGeneric.Object);
            await orderEventObserver.ExecuteFault(bhContextGeneric.Object, new Exception());

            // Assert
            orderAuditRegistryMock.Verify(s => s.Add(orderSaga, It.IsAny<string>(), It.IsAny<string>(), hook, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
