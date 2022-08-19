using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Web.Hubs
{
    /// <summary>
    /// Test for order hub instantiation
    /// </summary>
    [TestClass]
    public class OrderHubTest
    {
        private class DummyLogger : ILogger<OrderHub>
        {
            public string? LastMessage { get; private set; }
            
            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                LastMessage = formatter.Invoke(state, exception);
            }
        }

        /// <summary>
        /// Test connect to client
        /// </summary>
        [TestMethod]
        public void ConnectToClient()
        {
            var mockContext = new Mock<HubCallerContext>();
            var dummyLogger = new DummyLogger();
            var orderHUb = new OrderHub(dummyLogger);
            orderHUb.Context = mockContext.Object;

            string connectionId = "1337";
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            orderHUb.OnConnectedAsync();
            Assert.AreEqual($"Client connected. Connection ID: {connectionId}", dummyLogger.LastMessage);
        }

        /// <summary>
        /// Test disconnect from client
        /// </summary>
        [TestMethod]
        public void DisconectClient_NoException()
        {
            var mockContext = new Mock<HubCallerContext>();
            var dummyLogger = new DummyLogger();
            var orderHUb = new OrderHub(dummyLogger);
            orderHUb.Context = mockContext.Object;

            string connectionId = "1337";
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            orderHUb.OnDisconnectedAsync(null);
            Assert.AreEqual($"Client disconnected. Connection ID: {connectionId}", dummyLogger.LastMessage);
        }

        /// <summary>
        /// Test discinnect from client with exception
        /// </summary>
        [TestMethod]
        public void DisconectClient_WithException()
        {
            var mockContext = new Mock<HubCallerContext>();
            var dummyLogger = new DummyLogger();
            var orderHUb = new OrderHub(dummyLogger);
            orderHUb.Context = mockContext.Object;

            string connectionId = "1337";
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            var exception = new Exception("Test");
            orderHUb.OnDisconnectedAsync(exception);
            Assert.AreEqual($"Client disconnected. Connection ID: {connectionId}. Exception: {exception}", dummyLogger.LastMessage);
        }
    }
}
