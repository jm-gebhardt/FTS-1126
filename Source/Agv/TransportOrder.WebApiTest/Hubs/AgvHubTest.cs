using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs
{
    /// <summary>
    /// Test for agv hub instantiation
    /// </summary>
    [TestClass]
    public class AgvHubTest
    {
        private class DummyLogger : ILogger<AgvHub>
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
            var agvHub = new AgvHub(dummyLogger);
            agvHub.Context = mockContext.Object;

            const string connectionId = "1337";
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            agvHub.OnConnectedAsync();
            Assert.AreEqual($"Client connected. Connection ID: {connectionId}", dummyLogger.LastMessage);
        }

        /// <summary>
        /// Test disconnect from client
        /// </summary>
        [TestMethod]
        public void DisconnectClient_NoException()
        {
            var mockContext = new Mock<HubCallerContext>();
            var dummyLogger = new DummyLogger();
            var agvHub = new AgvHub(dummyLogger);
            agvHub.Context = mockContext.Object;

            const string connectionId = "1337";
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            agvHub.OnDisconnectedAsync(null);
            Assert.AreEqual($"Client disconnected. Connection ID: {connectionId}", dummyLogger.LastMessage);
        }

        /// <summary>
        /// Test disconnect from client with exception
        /// </summary>
        [TestMethod]
        public void DisconnectClient_WithException()
        {
            var mockContext = new Mock<HubCallerContext>();
            var dummyLogger = new DummyLogger();
            var agvHub = new AgvHub(dummyLogger);
            agvHub.Context = mockContext.Object;

            const string connectionId = "1337";
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            var exception = new Exception("Test");
            agvHub.OnDisconnectedAsync(exception);
            Assert.AreEqual($"Client disconnected. Connection ID: {connectionId}", dummyLogger.LastMessage);
        }
    }
}
