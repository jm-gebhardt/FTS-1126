using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class AgvGraphConsumerTestFixture : MqttConsumerTestFixture<MqttGraphConsumer>
{
    private IAgvRepository AgvRepository { get; } = new AgvRepositoryTestFixture().AgvRepository;

    private IGraphRepository GraphRepository { get; } = new GraphRepositoryTestFixture().GraphRepository;

    public AgvGraphConsumerTestFixture()
    {
        AddServices(sc => sc.AddSingleton(_ => AgvRepository));
        AddServices(sc => sc.AddSingleton(_ => GraphRepository));
        Build();
    }
}