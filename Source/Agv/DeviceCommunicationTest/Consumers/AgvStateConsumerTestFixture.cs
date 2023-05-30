using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    public class AgvStateConsumerTestFixture : MqttConsumerTestFixture<AgvStateConsumer>
    {
        private IAgvRegistry AgvRegistry { get; }

        public AgvStateConsumerTestFixture()
        {
            AgvRegistry = new AgvRegistry();
        }

        public AgvStateConsumerTestFixture WithAgvInformation(AgvInformation agvInformation)
        {
            AgvRegistry.AddOrUpdate(agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId, agvInformation);
            return this;
        }

        public AgvStateConsumerTestFixture UseDefaultServices()
        {
            AddServices(sc => sc.AddSingleton(_ => AgvRegistry));
            return this;
        }

        public AgvStateConsumerTestFixture Build()
        {
            base.Build();
            return this;
        }

        public AgvStateConsumerTestFixture EmptyRegistry()
        {
            var agvRegistryMock = new Mock<IAgvRegistry>();
            AddServices(sc => sc.AddSingleton(_ => agvRegistryMock.Object));
            return this;
        }
    }
}