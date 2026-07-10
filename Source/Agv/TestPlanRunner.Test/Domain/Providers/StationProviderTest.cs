using Shouldly;

namespace TestPlanRunner.Test.Domain.Providers;

[TestClass]
public class StationProviderTest
{
    [TestMethod]
    public void StationExists_KnownLabelDifferentCase_True()
    {
        var fixture = new StationProviderTestFixture()
            .WithStations(StationProviderTestFixture.StationName);

        fixture.StationProvider.StationExists(StationProviderTestFixture.StationName).ShouldBeTrue();
    }

    [TestMethod]
    public void StationExists_UnknownLabel_False()
    {
        var fixture = new StationProviderTestFixture()
            .WithStations(StationProviderTestFixture.StationName);

        fixture.StationProvider.StationExists(StationProviderTestFixture.UnknownStation).ShouldBeFalse();
    }

    [TestMethod]
    public void StationHasAnyType_PrimaryType_True()
    {
        var fixture = new StationProviderTestFixture()
            .WithTypedStation(additionalTypes: StationProviderTestFixture.AdditionalType);

        fixture.StationProvider.StationHasAnyType(StationProviderTestFixture.StationName, StationProviderTestFixture.PrimaryType).ShouldBeTrue();
    }

    [TestMethod]
    public void StationHasAnyType_AdditionalType_True()
    {
        var fixture = new StationProviderTestFixture()
            .WithTypedStation(additionalTypes: StationProviderTestFixture.AdditionalType);

        fixture.StationProvider.StationHasAnyType(StationProviderTestFixture.StationName, StationProviderTestFixture.AdditionalType).ShouldBeTrue();
    }

    [TestMethod]
    public void StationHasAnyType_MissingType_False()
    {
        var fixture = new StationProviderTestFixture()
            .WithTypedStation(additionalTypes: StationProviderTestFixture.AdditionalType);

        fixture.StationProvider.StationHasAnyType(StationProviderTestFixture.StationName, StationProviderTestFixture.MissingType).ShouldBeFalse();
    }

    [TestMethod]
    public void StationHasAnyType_UnknownStation_False()
    {
        var fixture = new StationProviderTestFixture()
            .WithTypedStation();

        fixture.StationProvider.StationHasAnyType(StationProviderTestFixture.UnknownStation, StationProviderTestFixture.PrimaryType).ShouldBeFalse();
    }
}