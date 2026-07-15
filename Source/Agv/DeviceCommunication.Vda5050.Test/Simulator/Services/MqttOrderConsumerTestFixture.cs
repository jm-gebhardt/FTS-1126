using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.MqttMessageHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class MqttOrderConsumerTestFixture : MqttConsumerTestFixture<MqttOrderConsumer>
{
    public MqttOrderConsumerTestFixture()
    {
        Build();
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureAutoMapper([typeof(Vda5050.Simulator.Messages.Profiles.OrderProfile).Assembly]);
    }
}