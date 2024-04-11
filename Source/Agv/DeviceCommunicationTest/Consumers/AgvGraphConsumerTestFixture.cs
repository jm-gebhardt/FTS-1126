using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Graphs;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

public class AgvGraphConsumerTestFixture : MqttConsumerTestFixture<AgvGraphConsumer>
{
    private IAgvRepository AgvRepository { get; } = new AgvRepositoryTestFixture().AgvRepository;

    public AgvGraphConsumerTestFixture()
    {
        AddServices(sc => sc.AddSingleton(_ => AgvRepository));
        Build();
    }
}