using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain.SimulatedAgvTypeConfigurationValidatorTestFixture;
using Generator = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator.SimulatedAgvTypeConfigurationGenerator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain;

[TestClass]
public class SimulatedAgvTypeConfigurationValidatorTest
{
    [TestMethod]
    public void Validate_ValidData_IsValid()
    {
        var fixture = new Fixture();

        var result = fixture.Sut.Validate(Generator.Generate(noOfAgvs: Fixture.MaxAgvs));

        result.IsValid.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(Fixture.IdentifierWithoutSeparator)]
    [DataRow(Fixture.IdentifierWithTrailingSeparator)]
    [DataRow(Fixture.IdentifierWithLeadingSeparator)]
    [DataRow(Fixture.Empty)]
    public void Validate_AgvTypeIdentifierWithoutManufacturerAndSeriesName_IsInvalid(string agvTypeIdentifier)
    {
        var fixture = new Fixture();

        var result = fixture.Sut.Validate(Generator.Generate(agvTypeIdentifier));

        result.IsValid.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow(Fixture.NegativeNoOfAgvs)]
    [DataRow(Fixture.TooManyAgvs)]
    public void Validate_NoOfAgvsOutOfRange_IsInvalid(int noOfAgvs)
    {
        var fixture = new Fixture();

        var result = fixture.Sut.Validate(Generator.Generate(noOfAgvs: noOfAgvs));

        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(SimulatedAgvTypeConfiguration.NoOfAgvs));
    }

    [TestMethod]
    [DataRow(Fixture.NoAgvs)]
    [DataRow(Fixture.MaxAgvs)]
    public void Validate_NoOfAgvsAtRangeLimit_IsValid(int noOfAgvs)
    {
        var fixture = new Fixture();

        var result = fixture.Sut.Validate(Generator.Generate(noOfAgvs: noOfAgvs));

        result.IsValid.ShouldBeTrue();
    }

    [TestMethod]
    public void Validate_StateUpdateIntervalBelowMinimum_IsInvalid()
    {
        var fixture = new Fixture();

        var result = fixture.Sut.Validate(Generator.Generate(stateUpdateIntervalMs: Fixture.TooShortIntervalMs));

        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(SimulatedAgvTypeConfiguration.StateUpdateIntervalMs));
    }

    [TestMethod]
    public void Validate_VisualizationUpdateIntervalBelowMinimum_IsInvalid()
    {
        var fixture = new Fixture();

        var result = fixture.Sut.Validate(Generator.Generate(visualizationUpdateIntervalMs: Fixture.TooShortIntervalMs));

        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(SimulatedAgvTypeConfiguration.VisualizationUpdateIntervalMs));
    }

    [TestMethod]
    public void Validate_UpdateIntervalsAtMinimum_IsValid()
    {
        var fixture = new Fixture();

        var result = fixture.Sut.Validate(Generator.Generate(stateUpdateIntervalMs: Fixture.MinIntervalMs,
            visualizationUpdateIntervalMs: Fixture.MinIntervalMs));

        result.IsValid.ShouldBeTrue();
    }
}