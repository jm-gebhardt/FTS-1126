using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Extensions;

public class SimulatedAgvTypeConfigurationExtensionsTestFixture
{
    private const int SpreadSpan = 2 * InitialBatteryChargeDeviation + 1;

    public const string MapId = "map";
    public const string AgvTypeIdentifier = SimulatedAgvTypeConfigurationGenerator.AgvTypeIdentifier;
    public const string Manufacturer = SimulatedAgvTypeConfigurationGenerator.Manufacturer;
    public const string FirstSerialNumber = SimulatedAgvTypeConfigurationGenerator.FirstSerialNumber;
    public const string SecondSerialNumber = SimulatedAgvTypeConfigurationGenerator.SecondSerialNumber;
    public const string ThirdSerialNumber = SimulatedAgvTypeConfigurationGenerator.ThirdSerialNumber;
    public const string FourthSerialNumber = SimulatedAgvTypeConfigurationGenerator.FourthSerialNumber;
    public const string SerialNumberWithoutIndex = $"{SimulatedAgvTypeConfigurationGenerator.SeriesName}x";
    public const int StateUpdateIntervalMs = SimulatedAgvTypeConfigurationGenerator.StateUpdateIntervalMs;
    public const int VisualizationUpdateIntervalMs = SimulatedAgvTypeConfigurationGenerator.VisualizationUpdateIntervalMs;
    public const double BatteryDischargeRate = SimulatedAgvTypeConfigurationGenerator.BatteryDischargeRate;
    public const double BatteryChargeRate = SimulatedAgvTypeConfigurationGenerator.BatteryChargeRate;
    public const int InitialBatteryCharge = SimulatedAgvTypeConfigurationGenerator.InitialBatteryCharge;
    public const int InitialBatteryChargeDeviation = SimulatedAgvTypeConfigurationGenerator.InitialBatteryChargeDeviation;
    public const int FirstSpreadBatteryCharge = InitialBatteryCharge;
    public const int SecondSpreadBatteryCharge = InitialBatteryCharge - 1;
    public const int ThirdSpreadBatteryCharge = InitialBatteryCharge + 1;
    public const int FourthSpreadBatteryCharge = InitialBatteryCharge - 2;
    public const int NoBatteryChargeDeviation = 0;
    public const int NoOfAgvs = 2;
    public const int ChangedNoOfAgvs = 7;
    public const int ChangedStateUpdateIntervalMs = 2500;
    public const int ChangedVisualizationUpdateIntervalMs = 400;
    public const double ChangedBatteryDischargeRate = 0.75;
    public const double ChangedBatteryChargeRate = 3.5;
    public const int ChangedInitialBatteryCharge = 90;
    public const int ChangedInitialBatteryChargeDeviation = 2;

    public static readonly string SerialNumberBeyondSpread = $"{SimulatedAgvTypeConfigurationGenerator.SeriesName}{SpreadSpan}";

    private int _noOfAgvs;
    private int _initialBatteryChargeDeviation = InitialBatteryChargeDeviation;

    public SimulatedAgvTypeConfiguration Sut => SimulatedAgvTypeConfigurationGenerator.Generate(noOfAgvs: _noOfAgvs,
        initialBatteryChargeDeviation: _initialBatteryChargeDeviation);

    public SimulatedAgvTypeConfigurationExtensionsTestFixture WithNoOfAgvs(int noOfAgvs)
    {
        _noOfAgvs = noOfAgvs;
        return this;
    }

    public SimulatedAgvTypeConfigurationExtensionsTestFixture WithInitialBatteryChargeDeviation(int initialBatteryChargeDeviation)
    {
        _initialBatteryChargeDeviation = initialBatteryChargeDeviation;
        return this;
    }
}