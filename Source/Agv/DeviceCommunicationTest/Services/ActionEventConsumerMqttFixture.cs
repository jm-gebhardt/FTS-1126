using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services;

public class ActionEventConsumerMqttFixture : MqttConsumerTestFixture<ActionAbortedConsumer>
{
    private readonly EfAgvRepositoryTestFixture _agvRepositoryTestFixture = new();
    private readonly Mock<IActionRepository> _actionRepository = new();

    public ActionEventConsumerMqttFixture()
    {
        AddServices(s => s.AddTransient(_ => _agvRepositoryTestFixture.Repository));
        AddServices(s => s.AddTransient(_ => _actionRepository.Object));

        Build();
    }

    public async Task AddAgv(AgvInformation agvInfo)
    {
        await _agvRepositoryTestFixture.Repository.AddOrUpdate(agvInfo.AgvStatusReceived!.CorrelationId, agvInfo.AgvStatusReceived.SerialNumber);
    }

    public ActionEventConsumerMqttFixture WithVehicleOrderId(Guid? id)
    {
        _actionRepository.Setup(x => x.GetVehicleOrderId(It.IsAny<Guid>())).ReturnsAsync(id);
        return this;
    }
}