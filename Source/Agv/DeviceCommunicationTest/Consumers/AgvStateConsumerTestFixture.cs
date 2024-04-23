using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class AgvStateConsumerTestFixture : MqttConsumerTestFixture<AgvStateConsumer>
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