using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.MqttMessageHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

public class MqttStateConsumerTestFixture : MqttConsumerTestFixture<MqttStateConsumer>
{
    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IFactsheetRequestCacheProvider> _factsheetRequestCache = new();

    public MqttStateConsumerTestFixture()
    {
        _factsheetRequestCache
            .Setup(x => x.HasAgvRequestedFactsheet(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvRepository.Object);
            sc.AddTransient(_ => _factsheetRequestCache.Object);
        });
        Build();
    }

    public MqttStateConsumerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetRequiredAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    public MqttStateConsumerTestFixture SetFactsheetNotYetRequested()
    {
        _factsheetRequestCache
            .Setup(x => x.HasAgvRequestedFactsheet(It.IsAny<Guid>()))
            .ReturnsAsync(false);
        return this;
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.ConfigureAutoMapper([typeof(CargoProfile).Assembly]);
    }
}