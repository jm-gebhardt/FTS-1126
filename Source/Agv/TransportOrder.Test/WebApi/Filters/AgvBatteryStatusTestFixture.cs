using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Filters;

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
        _significanceCheckerMock.Setup(x => x.HasSignificantChanges(It.IsAny<AgvBatteryStatusDto>(), It.IsAny<AgvBatteryStatusDto>()))
            .Returns(result);
        return this;
    }

    private AgvBatteryStatusTestFixture WithCurrentData(AgvBatteryStatusDto data)
    {
        Filter = new AgvBatteryStatusChangedFilter(_significanceCheckerMock.Object, new SemaphoreProvider<AgvBatteryStatusChangedFilter>(), data);
        return this;
    }

    public AgvBatteryStatusTestFixture WithPreviousDataFrom(DateTime date) => WithCurrentData(new AgvBatteryStatusDto { MessageCreatedDate = date });
}