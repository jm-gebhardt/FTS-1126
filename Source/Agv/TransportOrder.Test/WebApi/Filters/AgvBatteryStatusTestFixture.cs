using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Filters;

public class AgvBatteryStatusTestFixture
{
    private readonly Mock<ISignificanceChecker<AgvBatteryStatusDto>> _significanceCheckerMock;

    public AgvBatteryStatusChangedFilter Filter { get; private set; } = null!;

    public AgvBatteryStatusTestFixture()
    {
        _significanceCheckerMock = new Mock<ISignificanceChecker<AgvBatteryStatusDto>>();
        WithCurrentData(new AgvBatteryStatusDto { BatteryCharge = 50, MessageCreatedDate = DateTime.UtcNow });
    }

    public AgvBatteryStatusTestFixture WithHasSignificantChanges(bool result)
    {
        _significanceCheckerMock.Setup(x => x.HasSignificantChanges(It.IsAny<AgvBatteryStatusDto>(),
                It.IsAny<AgvBatteryStatusDto>()))
            .Returns(result);
        return this;
    }

    private AgvBatteryStatusTestFixture WithCurrentData(AgvBatteryStatusDto data)
    {
        Filter = new AgvBatteryStatusChangedFilter(_significanceCheckerMock.Object,
            new SemaphoreProvider<AgvBatteryStatusChangedFilter>(new Mock<ILogger<SemaphoreProvider<AgvBatteryStatusChangedFilter>>>().Object),
            new AgvBatteryConfiguration(0.3, 0.2, 0.5, 0.5), data);
        return this;
    }

    public AgvBatteryStatusTestFixture WithPreviousDataFrom(DateTime date) => WithCurrentData(new AgvBatteryStatusDto { MessageCreatedDate = date });
}