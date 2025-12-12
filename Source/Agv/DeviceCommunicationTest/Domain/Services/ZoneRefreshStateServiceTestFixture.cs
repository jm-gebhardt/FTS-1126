using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Domain.Services;

public sealed class ZoneRefreshStateServiceTestFixture : FixtureBase
{
    public readonly IZoneRefreshStateService Service;

    public ZoneRefreshStateServiceTestFixture()
    {
        AddServices(s =>
            {
                s.AddTransient<IZoneRefreshStateService, ZoneRefreshStateService>();
                s.AddTransient(_ => new Mock<ILogger<ZoneRefreshStateService>>().Object);
            }
            );

        Build();
        Service = Provider.GetRequiredService<IZoneRefreshStateService>();
    }
}