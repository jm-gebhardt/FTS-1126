using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class StartActionCommandConsumerTestFixture : MqttConsumerTestFixture<StartActionCommandConsumer>
{
    private readonly IGraphRepository _graphRepository = new GraphRepositoryTestFixture().WithGraphRepository().GraphRepository;

    public StartActionCommandConsumerTestFixture()
    {
        AddServices(s => s.AddTransient<IAgvRepository>(_ => Mock.Of<IAgvRepository>()));
        AddServices(s => s.AddTransient<IGraphRepository>(_ => _graphRepository));
        Build();
    }
}