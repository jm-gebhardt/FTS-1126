using Gebhardt.Acc.Common.Domain.Utilities;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain.SimulatedAgvTypeConfigurationTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain;

[TestClass]
public class SimulatedAgvTypeConfigurationTest
{
    [TestMethod]
    public void Id_ValidData_DerivedFromAgvTypeIdentifier()
    {
        var configuration = new Fixture().Sut;

        configuration.Id.ShouldBe(Fixture.AgvTypeIdentifier.ToGuid());
    }

    [TestMethod]
    [DataRow(Fixture.AgvTypeIdentifier, Fixture.Manufacturer, Fixture.SeriesName)]
    [DataRow(Fixture.IdentifierWithoutSeparator, Fixture.Manufacturer, Fixture.Empty)]
    [DataRow(Fixture.IdentifierWithTrailingSeparator, Fixture.Manufacturer, Fixture.Empty)]
    [DataRow(Fixture.IdentifierWithLeadingSeparator, Fixture.Empty, Fixture.SeriesName)]
    [DataRow(Fixture.IdentifierWithAdditionalSeparator, Fixture.Manufacturer, Fixture.SeriesNameWithAdditionalSeparator)]
    [DataRow(Fixture.Empty, Fixture.Empty, Fixture.Empty)]
    public void ManufacturerAndSeriesName_VariousAgvTypeIdentifiers_SplitAtFirstSeparator(string agvTypeIdentifier, string expectedManufacturer, string expectedSeriesName)
    {
        var configuration = new Fixture()
            .WithAgvTypeIdentifier(agvTypeIdentifier)
            .Sut;

        configuration.Manufacturer.ShouldBe(expectedManufacturer);
        configuration.SeriesName.ShouldBe(expectedSeriesName);
    }

    [TestMethod]
    public void SerialNumbers_MultipleAgvs_SeriesNameWithZeroBasedIndexPerAgv()
    {
        var configuration = new Fixture()
            .WithNoOfAgvs(Fixture.NoOfAgvs)
            .Sut;

        string[] serialNumbers = configuration.SerialNumbers().ToArray();

        serialNumbers.ShouldBe([Fixture.FirstSerialNumber, Fixture.SecondSerialNumber, Fixture.ThirdSerialNumber]);
    }

    [TestMethod]
    public void SerialNumbers_NoAgvs_ReturnsEmpty()
    {
        var configuration = new Fixture().Sut;

        configuration.SerialNumbers().ShouldBeEmpty();
    }
}