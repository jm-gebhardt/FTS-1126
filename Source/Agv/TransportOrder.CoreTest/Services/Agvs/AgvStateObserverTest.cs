using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
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
        public async Task WhenAgvSagaStateChanged_AgvStateChangedPublished()
        {
            var agv = (AgvSaga) AgvGenerator.GenerateIdleAgv();

            var mockLogger = new Mock<ILogger<AgvStateMachine>>();
            var agvDataRegistryMock = new Mock<IAgvDataRegistry>();
            var mapRegistryMock = new Mock<IMapRegistry>();
            var graphRegistryMock = new Mock<IAgvGraphRegistry>();
            var mockContext = new Mock<BehaviorContext<AgvSaga>>();
            var agvStateObserver = new AgvStateObserver();
            var settingsProviderMock = new Mock<ISettingsProvider<AgvSettings>>();
            var agvStateMachine = new AgvStateMachine(
                mockLogger.Object,
                agvDataRegistryMock.Object,
                mapRegistryMock.Object,
                agvStateObserver,
                new ProductionOptions {ApplicationName = null, AutoAccept = true},
                settingsProviderMock.Object,
                graphRegistryMock.Object);

            var previousState = agvStateMachine.Idle;
            var currentState = agvStateMachine.Busy;

            mockContext.Setup(m => m.StateMachine.Initial).Returns(agvStateMachine.Initial);
            mockContext.Setup(m => m.Saga).Returns(agv);

            await agvStateObserver.StateChanged(mockContext.Object, currentState, previousState);
        }
    }
}