using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class RequestAgvMapConsumerTestFixture : MassTransitConsumerTestFixture<RequestAgvMapConsumer>
{
    public RequestAgvMapConsumerTestFixture()
    {
        Build();
    }
}