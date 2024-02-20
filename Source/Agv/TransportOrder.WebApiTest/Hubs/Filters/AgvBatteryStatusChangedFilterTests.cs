using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs.Filters;

[TestClass]
public class AgvBatteryStatusChangedFilterTests
{
    [TestMethod]
    public void ShouldPublish_WhenSignificantChangeDetected_ReturnsTrue()
    {
        var fixture = new AgvBatteryStatusTestFixture()
            .WithPreviousDataFrom(DateTime.UtcNow)
            .WithHasSignificantChanges(true);

        var data = AgvBatteryStatusDtoGenerator.Generate(DateTime.UtcNow.AddSeconds(1));
        fixture.Filter.ShouldPublish(data).ShouldBeTrue();
    }

    [TestMethod]
    public void ShouldPublish_WhenNoSignificantChangeDetected_ReturnsFalse()
    {
        var fixture = new AgvBatteryStatusTestFixture()
            .WithPreviousDataFrom(DateTime.UtcNow)
            .WithHasSignificantChanges(false);

        var data = AgvBatteryStatusDtoGenerator.Generate(DateTime.UtcNow.AddSeconds(1));
        fixture.Filter.ShouldPublish(data).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_WhenBatteryChargeDoesNotChangeSignificantly_ReturnsFalse()
    {
        var fixture = new AgvBatteryStatusTestFixture()
            .WithPreviousDataFrom(DateTime.Parse("2024-02-20T08:24:36.9265131Z"))
            .WithHasSignificantChanges(false);

        var data = AgvBatteryStatusDtoGenerator.Generate(DateTime.Parse("2024-02-20T08:24:37.9370972Z"));
        fixture.Filter.ShouldPublish(data).ShouldBeFalse();
    }

    [TestMethod]
    public void ShouldPublish_WhenMaxSilenceExceeded_ReturnsTrue()
    {
        var fixture = new AgvBatteryStatusTestFixture()
            .WithPreviousDataFrom(DateTime.UtcNow)
            .WithHasSignificantChanges(false);

        var data = AgvBatteryStatusDtoGenerator.Generate(DateTime.UtcNow.AddSeconds(30));
        fixture.Filter.ShouldPublish(data).ShouldBeTrue();
    }
}