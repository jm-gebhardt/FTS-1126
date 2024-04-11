using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

public class ActionEventConsumerMqttFixture : MqttConsumerTestFixture<ActionEventConsumer>
{
    private readonly AgvRepositoryTestFixture _agvRepositoryTestFixture = new();

    public ActionEventConsumerMqttFixture()
    {
        AddServices(s => s.AddTransient(_ => _agvRepositoryTestFixture.AgvRepository));

        Build();
    }

    public ActionEventConsumerMqttFixture WithAgvInformation(AgvInformation agvInformation)
    {
        _agvRepositoryTestFixture.WithAgvInformation(agvInformation);
        return this;
    }
}