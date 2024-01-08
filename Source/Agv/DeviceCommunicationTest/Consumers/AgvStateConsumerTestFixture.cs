using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ConvertConstructorToMemberInitializers
namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

public class AgvStateConsumerTestFixture : MqttConsumerTestFixture<AgvStateConsumer>
{
    private readonly IAgvRepository _agvRepository = new AgvRepository();

    public AgvStateConsumerTestFixture WithAgvInformation(AgvInformation agvInformation)
    {
        _agvRepository.AddOrUpdate(agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId, agvInformation);
        return this;
    }

    public AgvStateConsumerTestFixture UseDefaultServices()
    {
        AddServices(services =>
        {
            services.AddSingleton(_ => _agvRepository);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        });
        return this;
    }

    public AgvStateConsumerTestFixture Build()
    {
        base.Build();
        return this;
    }
}