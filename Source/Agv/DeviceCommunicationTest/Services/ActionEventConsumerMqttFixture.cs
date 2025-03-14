using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services;

public class ActionEventConsumerMqttFixture : MqttConsumerTestFixture<ActionAbortedConsumer>
{
    private readonly EfAgvRepositoryTestFixture _agvRepositoryTestFixture = new();

    public ActionEventConsumerMqttFixture()
    {
        AddServices(s => s.AddTransient(_ => _agvRepositoryTestFixture.Repository));

        Build();
    }

    public async Task AddAgv(AgvInformation agvInfo)
    {
        await _agvRepositoryTestFixture.Repository.AddOrUpdate(agvInfo.AgvStatusReceived!.CorrelationId, agvInfo.AgvStatusReceived.SerialNumber);
    }
}