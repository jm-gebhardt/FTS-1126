using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.Housekeeping;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Repositories;

[TestClass]
public class EfHousekeepingConfigurationRepositoryTest
{
    [TestMethod]
    public async Task GetConfigurationAsync_NoConfiguration_ReturnsNull()
    {
        var fixture = await new EfHousekeepingConfigurationRepositoryTestFixture()
            .BuildAsync();

        var result = await fixture.HousekeepingConfigurationRepository.GetConfigurationAsync(HousekeepingEntityType.Orders);

        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetConfigurationAsync_WithConfiguration_ReturnsConfiguration()
    {
        var configuration = new HousekeepingConfiguration(Guid.NewGuid(), HousekeepingEntityType.Orders, 7, HousekeepingInterval.Days, 3, HousekeepingInterval.Hours, 1, 1);

        var fixture = await new EfHousekeepingConfigurationRepositoryTestFixture()
            .WithConfiguration(configuration)
            .BuildAsync();

        var result = await fixture.HousekeepingConfigurationRepository.GetConfigurationAsync(configuration.EntityType);

        result.ShouldNotBeNull();
        result.OutdatedValue.ShouldBe(configuration.OutdatedValue);
        result.OutdatedInterval.ShouldBe(configuration.OutdatedInterval);
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_NoExistingConfiguration_AddsConfiguration()
    {
        var fixture = await new EfHousekeepingConfigurationRepositoryTestFixture()
            .BuildAsync();

        var configuration = new HousekeepingConfiguration(Guid.NewGuid(), HousekeepingEntityType.Orders, 14, HousekeepingInterval.Weeks, 3, HousekeepingInterval.Hours, 1, 1);
        await fixture.HousekeepingConfigurationRepository.AddOrUpdateAsync(configuration);

        var result = await fixture.HousekeepingConfigurationRepository.GetConfigurationAsync(configuration.EntityType);
        result.ShouldNotBeNull();
        result.OutdatedValue.ShouldBe(14);
        result.OutdatedInterval.ShouldBe(HousekeepingInterval.Weeks);
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_ExistingConfiguration_UpdatesConfiguration()
    {
        var initial = new HousekeepingConfiguration(Guid.NewGuid(), HousekeepingEntityType.Orders, 7, HousekeepingInterval.Days, 3, HousekeepingInterval.Hours, 1, 1);

        var fixture = await new EfHousekeepingConfigurationRepositoryTestFixture()
            .WithConfiguration(initial)
            .BuildAsync();

        var updated = new HousekeepingConfiguration(initial.Id, initial.EntityType, 30, HousekeepingInterval.Months, 3, HousekeepingInterval.Hours, 1, 1);
        await fixture.HousekeepingConfigurationRepository.AddOrUpdateAsync(updated);

        var result = await fixture.HousekeepingConfigurationRepository.GetConfigurationAsync(updated.EntityType);
        result.ShouldNotBeNull();
        result.OutdatedValue.ShouldBe(30);
        result.OutdatedInterval.ShouldBe(HousekeepingInterval.Months);
    }
}
