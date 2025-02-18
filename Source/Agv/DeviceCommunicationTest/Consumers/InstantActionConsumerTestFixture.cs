using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class InstantActionConsumerTestFixture : MqttConsumerTestFixture<InstantActionsConsumer>
{
    public InstantActionConsumerTestFixture()
    {
        Build();
    }
}