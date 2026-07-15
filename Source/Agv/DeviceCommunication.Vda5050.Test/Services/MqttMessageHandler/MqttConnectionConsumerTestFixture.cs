using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

public class MqttConnectionConsumerTestFixture: MqttConsumerTestFixture<MqttConnectionConsumer>
{
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public MqttConnectionConsumerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvRepository.Object);
        });
        _agvRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>())).Verifiable();
        Build();
    }

    public void VerifyAgvUpdated(Times times)
    {
        _agvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>()), times);
    }

    public MqttConnectionConsumerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetRequiredAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }
}