using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class HousekeepingConfigurationTest
{
    [TestMethod]
    public void DefaultConstructor_SetsEmptyValues()
    {
        var config = new HousekeepingConfiguration();

        config.Id.ShouldNotBe(Guid.Empty);
        config.OutdatedValue.ShouldBe(0);
        config.OutdatedInterval.ShouldBe(HousekeepingInterval.Hours);
    }

    [TestMethod]
    public void ConfigurationConstructor_ValidConfig_SetsProperties()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "HOUSEKEEPING:LOGS:OUTDATED_VALUE", "7" },
                { "HOUSEKEEPING:LOGS:OUTDATED_INTERVAL", "Weeks" }
            })
            .Build();

        var config = HousekeepingConfiguration.CreateDefault(HousekeepingEntityType.Logs, configuration);

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

        var config = HousekeepingConfiguration.CreateDefault(HousekeepingEntityType.Logs, configuration);

        config.OutdatedValue.ShouldBe(14);
        config.OutdatedInterval.ShouldBe(HousekeepingInterval.Days);
    }

    [TestMethod]
    public void ConfigurationConstructor_MissingConfig_ActionGroups_UsesDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var config = HousekeepingConfiguration.CreateDefault(HousekeepingEntityType.ActionGroups, configuration);

        config.OutdatedValue.ShouldBe(90);
        config.OutdatedInterval.ShouldBe(HousekeepingInterval.Days);
        config.ScheduleHour.ShouldBe(4);
    }

    [TestMethod]
    public void GetOutdatedTime_Hours_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), HousekeepingEntityType.Logs, 3, HousekeepingInterval.Hours, 3, HousekeepingInterval.Hours, 1, 1);
        var before = DateTime.UtcNow.AddHours(-3);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddHours(-3);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void GetOutdatedTime_Days_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), HousekeepingEntityType.Logs, 5, HousekeepingInterval.Days, 3, HousekeepingInterval.Hours, 1, 1);
        var before = DateTime.UtcNow.AddDays(-5);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddDays(-5);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void GetOutdatedTime_Weeks_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), HousekeepingEntityType.Logs, 2, HousekeepingInterval.Weeks, 3, HousekeepingInterval.Hours, 1, 1);
        var before = DateTime.UtcNow.AddDays(-14);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddDays(-14);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void GetOutdatedTime_Months_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), HousekeepingEntityType.Logs, 1, HousekeepingInterval.Months,3, HousekeepingInterval.Hours, 1, 1);
        var before = DateTime.UtcNow.AddMonths(-1);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddMonths(-1);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }

    [TestMethod]
    public void GetOutdatedTime_Years_ReturnsTimeInThePast()
    {
        var config = new HousekeepingConfiguration(Guid.NewGuid(), HousekeepingEntityType.Logs, 1, HousekeepingInterval.Years, 3, HousekeepingInterval.Hours, 1, 1);
        var before = DateTime.UtcNow.AddYears(-1);

        var result = config.GetOutdatedTime();

        var after = DateTime.UtcNow.AddYears(-1);
        result.ShouldBeGreaterThanOrEqualTo(before);
        result.ShouldBeLessThanOrEqualTo(after);
    }
}
