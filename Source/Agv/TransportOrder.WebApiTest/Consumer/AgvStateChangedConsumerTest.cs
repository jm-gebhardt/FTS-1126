using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer
{
    [TestClass]
    public class AgvStateChangedConsumerTest
    {
        private const string AgvId = "agv1";

        [TestMethod]
        [DataRow(nameof(AgvStateMachine.Initial), null)]
        [DataRow(nameof(AgvStateMachine.Discovered), nameof(AgvStateMachine.Initial))]
        [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Busy))]
        public async Task Receiving_AgvStateChanged_SendAgvStateToAllClients(string currentState, string? previousState)
        {
            bool isInitialState = currentState == nameof(AgvStateMachine.Initial);

            var agv = new Core.States.AgvSaga(AgvId.ToGuid(), AgvId, currentState);

            var fixture = new ConsumerHubTestFixture<AgvStateChangedConsumer, IAgvHubContext>();
            await fixture.TestHarness!.Bus.Publish(new AgvStateChanged(agv.CorrelationId, agv, previousState));

            (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
            (await fixture.ConsumerHarness!.Consumed.Any<AgvStateChanged>()).ShouldBeTrue();

            fixture.MockHub.Verify(h => h.SendAgvStateToAllClients(
                It.Is<IAgv>(o => o.CorrelationId == agv.CorrelationId), isInitialState), Times.Once);
        }
    }
}
