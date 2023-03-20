using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    public class AgvGraphConsumerTestFixture : MqttConsumerTestFixture<AgvGraphConsumer>
    {
        private IAgvRegistry AgvRegistry { get; }

        public AgvGraphConsumerTestFixture()
        {
            AgvRegistry = new AgvRegistryTestFixture().AgvRegistry;
        }
        
        public AgvGraphConsumerTestFixture UseDefaultServices()
        {
             AddServices(sc => sc.AddSingleton(_ => AgvRegistry));
             return this;
        }

        public AgvGraphConsumerTestFixture Build()
        {
            base.Build();
            return this;
        }
    }
}