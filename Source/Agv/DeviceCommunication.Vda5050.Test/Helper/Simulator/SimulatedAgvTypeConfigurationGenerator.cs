using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

[PublicAPI]
public static class SimulatedAgvTypeConfigurationGenerator
{
    public const string Manufacturer = "gebhardt";
    public const string SeriesName = "simagv";
    public const string AgvTypeIdentifier = $"{Manufacturer}.{SeriesName}";
    public const string OtherSeriesName = "simfork";
    public const string OtherAgvTypeIdentifier = $"{Manufacturer}.{OtherSeriesName}";
    public const string FirstSerialNumber = $"{SeriesName}0";
    public const string SecondSerialNumber = $"{SeriesName}1";
    public const string ThirdSerialNumber = $"{SeriesName}2";
    public const string FourthSerialNumber = $"{SeriesName}3";
    public const int StateUpdateIntervalMs = 1500;
    public const int VisualizationUpdateIntervalMs = 250;
    public const double BatteryDischargeRate = 0.25;
    public const double BatteryChargeRate = 2.5;
    public const int InitialBatteryCharge = 60;
    public const int InitialBatteryChargeDeviation = 10;

    public static SimulatedAgvTypeConfiguration Generate(string agvTypeIdentifier = AgvTypeIdentifier,
        int noOfAgvs = 0,
        int stateUpdateIntervalMs = StateUpdateIntervalMs,
        int visualizationUpdateIntervalMs = VisualizationUpdateIntervalMs,
        double batteryDischargeRate = BatteryDischargeRate,
        double batteryChargeRate = BatteryChargeRate,
        int initialBatteryCharge = InitialBatteryCharge,
        int initialBatteryChargeDeviation = InitialBatteryChargeDeviation)
    {
        return new SimulatedAgvTypeConfiguration(agvTypeIdentifier,
            noOfAgvs,
            stateUpdateIntervalMs,
            visualizationUpdateIntervalMs,
            batteryDischargeRate,
            batteryChargeRate,
            initialBatteryCharge,
            initialBatteryChargeDeviation);
    }
}