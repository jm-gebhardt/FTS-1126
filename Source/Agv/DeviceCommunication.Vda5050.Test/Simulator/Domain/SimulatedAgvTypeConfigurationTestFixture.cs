using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain;

public class SimulatedAgvTypeConfigurationTestFixture
{
    public const string Manufacturer = SimulatedAgvTypeConfigurationGenerator.Manufacturer;
    public const string SeriesName = SimulatedAgvTypeConfigurationGenerator.SeriesName;
    public const string AgvTypeIdentifier = SimulatedAgvTypeConfigurationGenerator.AgvTypeIdentifier;
    public const string IdentifierWithoutSeparator = Manufacturer;
    public const string IdentifierWithTrailingSeparator = $"{Manufacturer}.";
    public const string IdentifierWithLeadingSeparator = $".{SeriesName}";
    public const string IdentifierWithAdditionalSeparator = $"{AgvTypeIdentifier}.v2";
    public const string SeriesNameWithAdditionalSeparator = $"{SeriesName}.v2";
    public const string Empty = "";
    public const string FirstSerialNumber = SimulatedAgvTypeConfigurationGenerator.FirstSerialNumber;
    public const string SecondSerialNumber = SimulatedAgvTypeConfigurationGenerator.SecondSerialNumber;
    public const string ThirdSerialNumber = SimulatedAgvTypeConfigurationGenerator.ThirdSerialNumber;
    public const int NoOfAgvs = 3;

    private string _agvTypeIdentifier = AgvTypeIdentifier;
    private int _noOfAgvs;

    public SimulatedAgvTypeConfiguration Sut => SimulatedAgvTypeConfigurationGenerator.Generate(_agvTypeIdentifier, _noOfAgvs);

    public SimulatedAgvTypeConfigurationTestFixture WithAgvTypeIdentifier(string agvTypeIdentifier)
    {
        _agvTypeIdentifier = agvTypeIdentifier;
        return this;
    }

    public SimulatedAgvTypeConfigurationTestFixture WithNoOfAgvs(int noOfAgvs)
    {
        _noOfAgvs = noOfAgvs;
        return this;
    }
}