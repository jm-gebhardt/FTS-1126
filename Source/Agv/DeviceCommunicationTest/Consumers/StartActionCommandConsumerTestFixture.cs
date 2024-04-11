using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

public class StartActionCommandConsumerTestFixture : MqttConsumerTestFixture<StartActionCommandConsumer>
{
    public StartActionCommandConsumerTestFixture()
    {
        AddServices(s => s.AddTransient<IAgvRepository>(_ => Mock.Of<IAgvRepository>()));
        Build();
    }
}