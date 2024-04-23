using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Services;
using Gebhardt.Acc.DeviceCommunication.Services.Graphs;
using Gebhardt.Acc.DeviceCommunicationTest.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class AgvGraphConsumerTestFixture : MqttConsumerTestFixture<AgvGraphConsumer>
{
    private IAgvRepository AgvRepository { get; } = new AgvRepositoryTestFixture().AgvRepository;

    public AgvGraphConsumerTestFixture()
    {
        AddServices(sc => sc.AddSingleton(_ => AgvRepository));
        Build();
    }
}