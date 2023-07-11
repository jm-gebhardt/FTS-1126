using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer
{
    [TestClass]
    public class AgvPropertyChangedConsumerTest
    {
        private const string AgvId = "agv1";

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Receiving_AgvPropertyChanged_SendAgvStateToAllClients(bool alive)
        {
            var fixture = new ConsumerHubTestFixture<AgvPropertyChangedConsumer, IAgvHubContext>();
            await fixture.TestHarness!.Bus.Publish(
                new AgvPropertyChanged(
                    new Core.States.AgvSaga(AgvId.ToGuid(), AgvId, nameof(AgvStateMachine.Idle)) { IsOnline = alive },
                    AgvId.ToGuid()
                ));

            (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
            (await fixture.ConsumerHarness!.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();

            fixture.MockHub.Verify(h => h.SendAgvStateToAllClients(
                It.Is<IAgv>(o => o.CorrelationId == AgvId.ToGuid()), false), Times.Once);
        }
    }
}
