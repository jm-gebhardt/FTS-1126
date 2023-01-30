using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Hubs
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

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
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
            dummyLogger.LastMessage.ShouldBe($"Client connected. Connection ID: {connectionId}");
        }

        /// <summary>
        /// Test disconnect from client
        /// </summary>
        [TestMethod]
        public void DisconnectClient_NoException()
        {
            var mockContext = new Mock<HubCallerContext>();
            var dummyLogger = new DummyLogger();
            var orderHUb = new OrderHub(dummyLogger);
            orderHUb.Context = mockContext.Object;

            string connectionId = "1337";
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            orderHUb.OnDisconnectedAsync(null);
            dummyLogger.LastMessage.ShouldBe($"Client disconnected. Connection ID: {connectionId}");
        }

        /// <summary>
        /// Test disconnect from client with exception
        /// </summary>
        [TestMethod]
        public void DisconnectClient_WithException()
        {
            var mockContext = new Mock<HubCallerContext>();
            var dummyLogger = new DummyLogger();
            var orderHUb = new OrderHub(dummyLogger);
            orderHUb.Context = mockContext.Object;

            string connectionId = "1337";
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            var exception = new Exception("Test");
            orderHUb.OnDisconnectedAsync(exception);
            dummyLogger.LastMessage.ShouldBe($"Client disconnected. Connection ID: {connectionId}");
        }
    }
}
