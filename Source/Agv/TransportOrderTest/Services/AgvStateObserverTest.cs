using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.Web;
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
        public async Task When_AgvSaga_State_Changed_AgvStateChanged_Published()
        {
            var mapRegistryMock = new Mock<IMapRegistry>();
            var mockHub = new Mock<IAgvHubContext>();
            var mockContext = new Mock<BehaviorContext<AgvSaga>>();
            var mockLogger = new Mock<ILogger<AgvStateMachine>>();
            var agvStateObserver = new AgvStateObserver(mockHub.Object);
            var agvStateMachine = new AgvStateMachine(mockLogger.Object, mapRegistryMock.Object, agvStateObserver);
            var previousState = agvStateMachine.Idle;
            var currentState = agvStateMachine.Reserved;

            mockContext.Setup(m => m.StateMachine.Initial).Returns(agvStateMachine.Initial);

            await agvStateObserver.StateChanged(mockContext.Object, currentState, previousState);

            mockHub.Verify(s => s.SendAgvToAllClients(nameof(Defines.HubMethods.AgvStateChanged), It.IsAny<IAgv>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// When_AgvSaga_Is_New_AddTrigger_Observer
        /// </summary>
        [TestMethod]
        public async Task When_AgvSaga_StateIsInitial_AgvDiscovered_Published()
        {
            var mapRegistryMock = new Mock<IMapRegistry>();
            var mockHub = new Mock<IAgvHubContext>();
            var mockContext = new Mock<BehaviorContext<AgvSaga>>();
            var mockLogger = new Mock<ILogger<AgvStateMachine>>();
            var agvStateObserver = new AgvStateObserver(mockHub.Object);
            var agvStateMachine = new AgvStateMachine(mockLogger.Object, mapRegistryMock.Object, agvStateObserver);
            var previousState = agvStateMachine.Idle;
            var currentState = agvStateMachine.Initial;

            mockContext.Setup(m => m.StateMachine.Initial).Returns(currentState);

            await agvStateObserver.StateChanged(mockContext.Object, currentState, previousState);

            mockHub.Verify(s => s.SendAgvToAllClients(nameof(Defines.HubMethods.AgvDiscovered), It.IsAny<IAgv>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
