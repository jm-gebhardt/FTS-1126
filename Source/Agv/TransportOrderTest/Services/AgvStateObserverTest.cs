using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// AgvStateObserver Test
    /// </summary>
    [TestClass]
    public class AgvStateObserverTest
    {
        /// <summary>
        /// When AgvSaga state has changed check if observer was triggered
        /// </summary>
        [TestMethod]
        public async Task When_AgvSaga_State_Changed_Trigger_Observer()
        {
            var mockHub = new Mock<IAgvHubContext>();
            var mockContext = new Mock<BehaviorContext<AgvSaga>>();
            var mockLogger = new Mock<ILogger<AgvStateMachine>>();
            var agvStateObserver = new AgvStateObserver(mockHub.Object);
            var agvStateMachine = new AgvStateMachine(mockLogger.Object, agvStateObserver);
            var previousState = agvStateMachine.Idle;
            var currentState = agvStateMachine.Reserved;

            await agvStateObserver.StateChanged(mockContext.Object, currentState, previousState);

            mockHub.Verify(s => s.SendAgvToAllClients("AgvStateChanged", It.IsAny<IAgv>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
