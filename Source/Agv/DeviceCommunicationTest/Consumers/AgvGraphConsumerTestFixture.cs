using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Graphs;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ConvertConstructorToMemberInitializers
namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

public class AgvGraphConsumerTestFixture : MqttConsumerTestFixture<AgvGraphConsumer>
{
    private IAgvRepository AgvRepository { get; } = new AgvRepositoryTestFixture().AgvRepository;

    public AgvGraphConsumerTestFixture UseDefaultServices()
    {
        AddServices(sc => sc.AddSingleton(_ => AgvRepository));
        return this;
    }

    public AgvGraphConsumerTestFixture Build()
    {
        base.Build();
        return this;
    }
}