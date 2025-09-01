using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Filters.SignificanceCheckers;

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
    public void HasSignificantChanges_WhenBatteryChargeDoesNotChangeSignificantly_ReturnsFalse()
    {
        var checker = new AgvBatteryStatusSignificanceChecker();
        var oldStatus = new AgvBatteryStatusDto
        {
            CorrelationId = Guid.Parse("14f7f5cd-efe9-5550-fce2-a60b86db78dc"),
            BatteryCharge = 71.28799999999876,
            IsCharging = false,
            BatteryVoltage = 47.912071228027344,
            BatteryCurrent = -2.0832419395446777,
            BatteryTemperature = 20.03445281982422,
            EstimatedFullyChargedTime = null,
            LastBatteryReplacement = DateTime.Parse("2024-02-18T11:49:55.0538627Z"),
            BatteryLevelCritical = 15,
            BatteryLevelLow = 40,
            MinChargeUp = 60,
            MaxChargeUp = 80,
            MessageCreatedDate = DateTime.Parse("2024-02-20T11:49:55.0753669Z")
        };
        var newStatus = new AgvBatteryStatusDto
        {
            CorrelationId = Guid.Parse("14f7f5cd-efe9-5550-fce2-a60b86db78dc"),
            BatteryCharge = 71.20799999999873,
            IsCharging = false,
            BatteryVoltage = 47.926395416259766,
            BatteryCurrent = -2.0769848823547363,
            BatteryTemperature = 19.955346298217773,
            EstimatedFullyChargedTime = null,
            LastBatteryReplacement = DateTime.Parse("2024-02-18T11:49:56.0625123Z"),
            BatteryLevelCritical = 15,
            BatteryLevelLow = 40,
            MinChargeUp = 60,
            MaxChargeUp = 80,
            MessageCreatedDate = DateTime.Parse("2024-02-20T11:49:56.0803932Z")
        };

        checker.HasSignificantChanges(oldStatus, newStatus).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenBatteryChargeChanges_ReturnsTrue()
    {
        var checker = new AgvBatteryStatusSignificanceChecker();
        var oldStatus = new AgvBatteryStatusDto {BatteryCharge = 0.5};
        var newStatus = new AgvBatteryStatusDto {BatteryCharge = 0.2};

        checker.HasSignificantChanges(oldStatus, newStatus).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenOnlyMessageCreatedDateChanges_ReturnsFalse()
    {
        var checker = new AgvBatteryStatusSignificanceChecker();
        var oldStatus = new AgvBatteryStatusDto {MessageCreatedDate = DateTime.Now};
        var newStatus = new AgvBatteryStatusDto {MessageCreatedDate = DateTime.Now.AddSeconds(10)};

        checker.HasSignificantChanges(oldStatus, newStatus).ShouldBeFalse();
    }
}