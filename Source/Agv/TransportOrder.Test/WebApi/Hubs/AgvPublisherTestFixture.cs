using AutoMapper;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Testing.Filters;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Hubs;

public class AgvPublisherTestFixture : PublisherFixture<AgvPublisher, AgvHub, IAgvClient>
{
    private readonly bool _useAutoMapper;
    private readonly Mock<IAgvDb> _agvDbMock = new();

    protected override IEnumerable<Profile> GetProfiles()
    {
        if (!_useAutoMapper)
        {
            yield break;
        }
        yield return new AgvProfile();
        yield return new ActionProfile();
        yield return new AgvErrorProfile();
        yield return new AgvGraphProfile();
        yield return new LogEntryProfile();
    }

    public AgvPublisherTestFixture(bool useAutoMapper = false)
    {
        _useAutoMapper = useAutoMapper;
        AddServices(services =>
        {
            services.AddSingleton<IMessageFilter<AgvBatteryStatusDto>>(new FilterMock<AgvBatteryStatusDto>(true));
            services.AddSingleton<IAgvDb>(_ => _agvDbMock.Object);
        });
        Build();
    }

    public AgvPublisherTestFixture WithAgvSettings(AgvSettingsReadModel? agvSettings = null)
    {
        agvSettings ??= new AgvSettingsReadModel
        {
            AgvOfflineTimeoutMsWhileIdle = 1000,
            AgvOfflineTimeoutMsWhileBusy = 2000,
            BatteryLevelCritical = 10,
            BatteryLevelLow = 20,
            MinChargeUp = 30,
            MaxChargeUp = 80,
            InfiniteCharging = true
        };
        var agvMock = new Mock<Agv>();
        _agvDbMock.Setup(db => db.Agvs.GetAgvAsync(It.IsAny<Guid>()))
            .ReturnsAsync(agvMock.Object);
        _agvDbMock.Setup(db => db.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(agvSettings);
        return this;
    }
}