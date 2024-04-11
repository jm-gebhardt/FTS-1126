using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

public class RequestAgvMapConsumerTestFixture : MassTransitConsumerTestFixture<RequestAgvMapConsumer>
{
    public RequestAgvMapConsumerTestFixture()
    {
        Build();
    }
}