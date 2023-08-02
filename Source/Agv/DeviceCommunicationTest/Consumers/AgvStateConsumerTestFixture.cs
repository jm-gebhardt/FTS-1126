using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ConvertConstructorToMemberInitializers
namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    public class AgvStateConsumerTestFixture : MqttConsumerTestFixture<AgvStateConsumer>
    {
        public IAgvRegistry AgvRegistry { get; } = new AgvRegistry();

        public AgvStateConsumerTestFixture WithAgvInformation(AgvInformation agvInformation)
        {
            AgvRegistry.AddOrUpdate(agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId, agvInformation);
            return this;
        }

        public AgvStateConsumerTestFixture UseDefaultServices()
        {
            AddServices(services =>
            {
                services.AddSingleton(_ => AgvRegistry);
                services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            });
            return this;
        }

        public AgvStateConsumerTestFixture Build()
        {
            base.Build();
            return this;
        }

        public AgvStateConsumerTestFixture EmptyRegistry()
        {
            AddServices(sc => sc.AddSingleton(_ => AgvRegistry));
            return this;
        }
    }
}