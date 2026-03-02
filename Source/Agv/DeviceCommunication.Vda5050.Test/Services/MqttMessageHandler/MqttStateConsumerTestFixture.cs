using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

public class MqttStateConsumerTestFixture : MqttConsumerTestFixture<MqttStateConsumer>
{
    public MqttStateConsumerTestFixture()
    {
        Build();
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
    }
}