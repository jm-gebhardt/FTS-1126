using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ConvertConstructorToMemberInitializers
namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

public class AgvStateConsumerTestFixture : MqttConsumerTestFixture<AgvStateConsumer>
{
    public IAgvRepository AgvRepository { get; } = new AgvRepository();

    public AgvStateConsumerTestFixture WithAgvInformation(AgvInformation agvInformation)
    {
        AgvRepository.AddOrUpdate(agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId, agvInformation);
        return this;
    }

    public AgvStateConsumerTestFixture UseDefaultServices()
    {
        AddServices(services =>
        {
            services.AddSingleton(_ => AgvRepository);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        });
        return this;
    }

    public AgvStateConsumerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public AgvStateConsumerTestFixture EmptyRepository()
    {
        AddServices(sc => sc.AddSingleton(_ => AgvRepository));
        return this;
    }
}