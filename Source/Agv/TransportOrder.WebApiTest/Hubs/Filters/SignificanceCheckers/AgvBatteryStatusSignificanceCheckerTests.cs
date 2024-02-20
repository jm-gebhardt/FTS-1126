using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs.Filters.SignificanceCheckers;

[TestClass]
public class AgvBatteryStatusSignificanceCheckerTests
{
    [TestMethod]
    public void HasSignificantChanges_WhenEqualObjects_ReturnsFalse()
    {
        var checker = new AgvBatteryStatusSignificanceChecker();
        var oldStatus = new AgvBatteryStatusDto();
        var newStatus = new AgvBatteryStatusDto();

        checker.HasSignificantChanges(oldStatus, newStatus).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenLastBatteryReplacementChanges_ReturnsTrue()
    {
        var checker = new AgvBatteryStatusSignificanceChecker();
        var oldStatus = new AgvBatteryStatusDto { LastBatteryReplacement = DateTime.UtcNow };
        var newStatus = new AgvBatteryStatusDto { LastBatteryReplacement = oldStatus.LastBatteryReplacement.Value.AddSeconds(10) };

        checker.HasSignificantChanges(oldStatus, newStatus).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenBatteryChargeChanges_ReturnsTrue()
    {
        var checker = new AgvBatteryStatusSignificanceChecker();
        var oldStatus = new AgvBatteryStatusDto { BatteryCharge = 0.5 };
        var newStatus = new AgvBatteryStatusDto { BatteryCharge = 0.2 };

        checker.HasSignificantChanges(oldStatus, newStatus).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenOnlyMessageCreatedDateChanges_ReturnsFalse()
    {
        var checker = new AgvBatteryStatusSignificanceChecker();
        var oldStatus = new AgvBatteryStatusDto { MessageCreatedDate = DateTime.Now };
        var newStatus = new AgvBatteryStatusDto { MessageCreatedDate = DateTime.Now.AddSeconds(10) };

        checker.HasSignificantChanges(oldStatus, newStatus).ShouldBeFalse();
    }
}