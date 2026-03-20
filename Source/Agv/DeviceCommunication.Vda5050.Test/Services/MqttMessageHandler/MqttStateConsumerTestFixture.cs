using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

public class MqttStateConsumerTestFixture : MqttConsumerTestFixture<MqttStateConsumer>
{
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public MqttStateConsumerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvRepository.Object);
        });
        Build();
    }

    public MqttStateConsumerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    }
}