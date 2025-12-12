using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Services;

public sealed class AgvSettingsServiceTestFixture: FixtureBase
{
    private readonly Mock<ITransportOrderAgvSettingsProvider> _settingsProvider = new();
    private readonly Mock<ITransportOrderAgvRepository> _agvRepository = new();

    public readonly AgvSettingsService Service;

    public AgvSettingsServiceTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvRepository.Object);
            s.AddTransient(_ => _settingsProvider.Object);
            s.AddTransient<AgvSettingsService>();
        });
        Build();
        _settingsProvider.Setup(x => x.InitializeSettingsGroups()).Verifiable();
        Service = Provider.GetRequiredService<AgvSettingsService>();
    }

    public AgvSettingsServiceTestFixture WithAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    public AgvSettingsServiceTestFixture WithSettings(AgvSettingsReadModel settings)
    {
        _settingsProvider.Setup(x => x.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(settings);
        return this;
    }
}