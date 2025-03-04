using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class AgvStateConsumerTestFixture : MqttConsumerTestFixture<MqttStateConsumer>
{
    private readonly IAgvRepository _agvRepository = new AgvRepositoryTestFixture().AgvRepository;

    public AgvStateConsumerTestFixture()
    {
        AddServices(services => services.AddSingleton(_ => _agvRepository));
        Build();
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    }

    public AgvStateConsumerTestFixture WithAgvInformation(AgvInformation agvInformation)
    {
        _agvRepository.AddOrUpdate(agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId, agvInformation);
        return this;
    }
}