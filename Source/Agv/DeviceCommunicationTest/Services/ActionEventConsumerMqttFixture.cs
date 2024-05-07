using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services;

public class ActionEventConsumerMqttFixture : MqttConsumerTestFixture<ActionAbortedConsumer>
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