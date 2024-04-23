using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class StartActionCommandConsumerTestFixture : MqttConsumerTestFixture<StartActionCommandConsumer>
{
    public StartActionCommandConsumerTestFixture()
    {
        AddServices(s => s.AddTransient<IAgvRepository>(_ => Mock.Of<IAgvRepository>()));
        Build();
    }
}