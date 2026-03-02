using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

public class MqttConnectionConsumerTestFixture: MqttConsumerTestFixture<MqttConnectionConsumer>
{
    public MqttConnectionConsumerTestFixture()
    {
        Build();
    }
}