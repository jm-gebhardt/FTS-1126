using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.MqttMessageHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class MqttInstantActionsConsumerTestFixture : MqttConsumerTestFixture<MqttInstantActionsConsumer>
{
    public MqttInstantActionsConsumerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddSingleton(new Options { Agvs = 1 });
        });
        Build();
    }
}