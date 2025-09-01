using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Filters;

[TestClass]
public class AgvBatteryStatusChangedFilterTests
{
    [TestMethod]
    public async Task ShouldPublish_WhenSignificantChangeDetected_ReturnsTrue()
    {
        var fixture = new AgvBatteryStatusTestFixture()
            .WithPreviousDataFrom(DateTime.UtcNow)
            .WithHasSignificantChanges(true);

        var data = AgvBatteryStatusDtoGenerator.Generate(DateTime.UtcNow.AddSeconds(1));
        (await fixture.Filter.ShouldActAsync(data)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ShouldPublish_WhenNoSignificantChangeDetected_ReturnsFalse()
    {
        var fixture = new AgvBatteryStatusTestFixture()
            .WithPreviousDataFrom(DateTime.UtcNow)
            .WithHasSignificantChanges(false);

        var data = AgvBatteryStatusDtoGenerator.Generate(DateTime.UtcNow.AddSeconds(1));
        (await fixture.Filter.ShouldActAsync(data)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task HasSignificantChanges_WhenBatteryChargeDoesNotChangeSignificantly_ReturnsFalse()
    {
        var fixture = new AgvBatteryStatusTestFixture()
            .WithPreviousDataFrom(DateTime.Parse("2024-02-20T08:24:36.9265131Z"))
            .WithHasSignificantChanges(false);

        var data = AgvBatteryStatusDtoGenerator.Generate(DateTime.Parse("2024-02-20T08:24:37.9370972Z"));
        (await fixture.Filter.ShouldActAsync(data)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task ShouldPublish_WhenMaxSilenceExceeded_ReturnsTrue()
    {
        var fixture = new AgvBatteryStatusTestFixture()
            .WithPreviousDataFrom(DateTime.UtcNow)
            .WithHasSignificantChanges(false);

        var data = AgvBatteryStatusDtoGenerator.Generate(DateTime.UtcNow.AddSeconds(30));
        (await fixture.Filter.ShouldActAsync(data)).ShouldBeTrue();
    }
}