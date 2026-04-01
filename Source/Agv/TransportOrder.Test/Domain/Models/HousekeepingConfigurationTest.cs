using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class HousekeepingConfigurationTest
{
    [TestMethod]
    public void DefaultConstructor_SetsDefaultValues()
    {
        var config = new HousekeepingConfiguration();

        config.Id.ShouldNotBe(Guid.Empty);
        config.OutdatedValue.ShouldBe(1);
        config.OutdatedInterval.ShouldBe(HousekeepingInterval.Days);
    }

    [TestMethod]
    public void ParameterizedConstructor_SetsProperties()
    {
        var id = Guid.NewGuid();

        var config = new HousekeepingConfiguration(id, 30, HousekeepingInterval.Months);

        config.Id.ShouldBe(id);
        config.OutdatedValue.ShouldBe(30);
        config.OutdatedInterval.ShouldBe(HousekeepingInterval.Months);
    }

    [TestMethod]
    public void ConfigurationConstructor_ValidConfig_SetsProperties()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "HOUSEKEEPING:OUTDATED_VALUE", "7" },
                { "HOUSEKEEPING:OUTDATED_INTERVAL", "Weeks" }
            })
            .Build();

        var config = new HousekeepingConfiguration(configuration);

        config.Id.ShouldNotBe(Guid.Empty);
        config.OutdatedValue.ShouldBe(7);
        config.OutdatedInterval.ShouldBe(HousekeepingInterval.Weeks);
    }

    [TestMethod]
    public void ConfigurationConstructor_MissingConfig_UsesDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var config = new HousekeepingConfiguration(configuration);

        config.OutdatedValue.ShouldBe(14);
        config.OutdatedInterval.ShouldBe(HousekeepingInterval.Days);
    }

    [TestMethod]
    public void GetOutdatedTime_Hours_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), 3, HousekeepingInterval.Hours);
        var before = DateTime.UtcNow.AddHours(-3);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddHours(-3);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void GetOutdatedTime_Days_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), 5, HousekeepingInterval.Days);
        var before = DateTime.UtcNow.AddDays(-5);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddDays(-5);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void GetOutdatedTime_Weeks_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), 2, HousekeepingInterval.Weeks);
        var before = DateTime.UtcNow.AddDays(-14);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddDays(-14);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void GetOutdatedTime_Months_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), 1, HousekeepingInterval.Months);
        var before = DateTime.UtcNow.AddMonths(-1);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddMonths(-1);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void GetOutdatedTime_Years_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), 1, HousekeepingInterval.Years);
        var before = DateTime.UtcNow.AddYears(-1);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddYears(-1);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }
}
