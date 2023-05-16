using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Agvs
{
    [TestClass]
    public class AgvStateObserverTest
    {
        /// <summary>
        /// When AgvSaga state has changed check if observer was triggered
        /// </summary>
        [TestMethod]
        public async Task When_AgvSaga_State_Changed_AgvStateChanged_Published()
        {
            var mockLogger = new Mock<ILogger<AgvStateMachine>>();
            var agvDataRegistryMock = new Mock<IAgvDataRegistry>();
            var mapRegistryMock = new Mock<IMapRegistry>();
            var graphRegistryMock = new Mock<IGraphRegistry>();
            var mockHub = new Mock<IAgvHubContext>();
            var mockContext = new Mock<BehaviorContext<AgvSaga>>();
            var agvStateObserver = new AgvStateObserver(mockHub.Object);
            var settingsProviderMock = new Mock<ISettingsProvider<AgvSettings>>();
            var agvStateMachine = new AgvStateMachine(
                mockLogger.Object,
                agvDataRegistryMock.Object,
                mapRegistryMock.Object,
                graphRegistryMock.Object,
                agvStateObserver,
                new ProductionOptions { ApplicationName = null, AutoAccept = true },
                settingsProviderMock.Object);

            var previousState = agvStateMachine.Idle;
            var currentState = agvStateMachine.Busy;

            mockContext.Setup(m => m.StateMachine.Initial).Returns(agvStateMachine.Initial);

            await agvStateObserver.StateChanged(mockContext.Object, currentState, previousState);

            mockHub.Verify(s => s.SendAgvStateToAllClients(nameof(Defines.HubMethods.AgvStateChanged), It.IsAny<IAgv>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task When_AgvSaga_StateIsInitial_AgvDiscovered_Published()
        {
            var mockLogger = new Mock<ILogger<AgvStateMachine>>();
            var agvDataRegistryMock = new Mock<IAgvDataRegistry>();
            var mapRegistryMock = new Mock<IMapRegistry>();
            var graphRegistryMock = new Mock<IGraphRegistry>();
            var mockHub = new Mock<IAgvHubContext>();
            var mockContext = new Mock<BehaviorContext<AgvSaga>>();
            var agvStateObserver = new AgvStateObserver(mockHub.Object);
            var settingsProviderMock = new Mock<ISettingsProvider<AgvSettings>>();
            var agvStateMachine = new AgvStateMachine(
                mockLogger.Object,
                agvDataRegistryMock.Object,
                mapRegistryMock.Object,
                graphRegistryMock.Object,
                agvStateObserver,
                new ProductionOptions { ApplicationName = null, AutoAccept = true },
                settingsProviderMock.Object);

            var previousState = agvStateMachine.Idle;
            var currentState = agvStateMachine.Initial;

            mockContext.Setup(m => m.StateMachine.Initial).Returns(currentState);

            await agvStateObserver.StateChanged(mockContext.Object, currentState, previousState);

            mockHub.Verify(s => s.SendAgvStateToAllClients(nameof(Defines.HubMethods.AgvDiscovered), It.IsAny<IAgv>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
