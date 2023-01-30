using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
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
            var mockHub = new Mock<IAgvHubContext>();
            var fixture = new MassTransitTestFixture<AgvPropertyChangedConsumer>(services =>
            {
                services.AddTransient(_ => mockHub.Object);
                services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            });

            await fixture.TestHarness!.Bus.Publish(
                new AgvPropertyChanged(
                    new Core.States.AgvSaga(AgvId.ToGuid(), AgvId, nameof(AgvStateMachine.Idle)) { Alive = alive },
                    AgvId.ToGuid()
                ));

            (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
            (await fixture.ConsumerHarness!.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();

            const string method = nameof(Defines.HubMethods.AgvStateChanged);
            mockHub.Verify(h => h.SendAgvStateToAllClients(method,
                                                           It.Is<IAgv>(o => o.CorrelationId == AgvId.ToGuid()),
                                                           It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
