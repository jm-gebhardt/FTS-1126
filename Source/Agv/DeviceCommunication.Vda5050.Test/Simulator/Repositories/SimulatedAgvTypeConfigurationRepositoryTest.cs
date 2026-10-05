using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Extensions;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Repositories.SimulatedAgvTypeConfigurationRepositoryTestFixture;
using Generator = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator.SimulatedAgvTypeConfigurationGenerator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Repositories;

[TestClass]
public class SimulatedAgvTypeConfigurationRepositoryTest
{
    [TestMethod]
    public async Task GetAsync_StoredConfiguration_ManufacturerAndSeriesNameMaterialized()
    {
        await using var fixture = new Fixture();
        await fixture.Sut.AddAsync(Generator.Generate(noOfAgvs: Fixture.NoOfAgvs));

        var configuration = await fixture.Sut.GetAsync(Fixture.AgvTypeIdentifier.ToGuid());

        configuration.ShouldNotBeNull();
        configuration.AgvTypeIdentifier.ShouldBe(Fixture.AgvTypeIdentifier);
        configuration.Manufacturer.ShouldBe(Fixture.Manufacturer);
        configuration.SeriesName.ShouldBe(Fixture.SeriesName);
    }

    [TestMethod]
    public async Task GetAsync_StoredConfiguration_SettingsMaterialized()
    {
        await using var fixture = new Fixture();
        await fixture.Sut.AddAsync(Generator.Generate(noOfAgvs: Fixture.NoOfAgvs));

        var configuration = await fixture.Sut.GetAsync(Fixture.AgvTypeIdentifier.ToGuid());

        configuration.ShouldNotBeNull();
        configuration.NoOfAgvs.ShouldBe(Fixture.NoOfAgvs);
        configuration.StateUpdateIntervalMs.ShouldBe(Generator.StateUpdateIntervalMs);
        configuration.VisualizationUpdateIntervalMs.ShouldBe(Generator.VisualizationUpdateIntervalMs);
        configuration.BatteryDischargeRate.ShouldBe(Generator.BatteryDischargeRate);
        configuration.BatteryChargeRate.ShouldBe(Generator.BatteryChargeRate);
        configuration.InitialBatteryCharge.ShouldBe(Generator.InitialBatteryCharge);
        configuration.InitialBatteryChargeDeviation.ShouldBe(Generator.InitialBatteryChargeDeviation);
    }

    [TestMethod]
    public async Task GetAllAsync_StoredConfigurations_ReturnsAllConfigurations()
    {
        await using var fixture = new Fixture();
        await fixture.Sut.AddAsync(Generator.Generate());
        await fixture.Sut.AddAsync(Generator.Generate(Fixture.OtherAgvTypeIdentifier));

        var configurations = await fixture.Sut.GetAllAsync();

        configurations.Select(c => c.AgvTypeIdentifier)
            .ShouldBe(new[] { Fixture.AgvTypeIdentifier, Fixture.OtherAgvTypeIdentifier }, true);
    }

    [TestMethod]
    public async Task UpdateAsync_StoredConfiguration_NewValuesReturned()
    {
        await using var fixture = new Fixture();
        await fixture.Sut.AddAsync(Generator.Generate(noOfAgvs: Fixture.NoOfAgvs));

        await fixture.Sut.UpdateAsync(Generator.Generate(noOfAgvs: Fixture.UpdatedNoOfAgvs));

        var configuration = await fixture.Sut.GetAsync(Fixture.AgvTypeIdentifier.ToGuid());
        configuration.ShouldNotBeNull();
        configuration.NoOfAgvs.ShouldBe(Fixture.UpdatedNoOfAgvs);
    }

    [TestMethod]
    public async Task UpdateAsync_UnknownAgvType_ThrowsAgvTypeConfigurationNotFoundException()
    {
        await using var fixture = new Fixture();

        await Should.ThrowAsync<AgvTypeConfigurationNotFoundException>(() => fixture.Sut.UpdateAsync(Generator.Generate()));
    }

    [TestMethod]
    public async Task DeleteAsync_StoredConfiguration_NoLongerReturned()
    {
        await using var fixture = new Fixture();
        await fixture.Sut.AddAsync(Generator.Generate());

        await fixture.Sut.DeleteAsync(Fixture.AgvTypeIdentifier.ToGuid());

        (await fixture.Sut.GetAsync(Fixture.AgvTypeIdentifier.ToGuid())).ShouldBeNull();
    }

    [TestMethod]
    public async Task DeleteAsync_UnknownAgvType_ThrowsAgvTypeConfigurationNotFoundException()
    {
        await using var fixture = new Fixture();

        await Should.ThrowAsync<AgvTypeConfigurationNotFoundException>(() => fixture.Sut.DeleteAsync(Fixture.AgvTypeIdentifier.ToGuid()));
    }

    [TestMethod]
    public async Task GetRequiredAsync_UnknownAgvType_ThrowsAgvTypeConfigurationNotFoundException()
    {
        await using var fixture = new Fixture();

        await Should.ThrowAsync<AgvTypeConfigurationNotFoundException>(() => fixture.Sut.GetRequiredAsync(Fixture.AgvTypeIdentifier.ToGuid()));
    }

    [TestMethod]
    public async Task GetRequiredAsync_StoredConfiguration_ReturnsConfiguration()
    {
        await using var fixture = new Fixture();
        await fixture.Sut.AddAsync(Generator.Generate());

        var configuration = await fixture.Sut.GetRequiredAsync(Fixture.AgvTypeIdentifier.ToGuid());

        configuration.AgvTypeIdentifier.ShouldBe(Fixture.AgvTypeIdentifier);
    }
}