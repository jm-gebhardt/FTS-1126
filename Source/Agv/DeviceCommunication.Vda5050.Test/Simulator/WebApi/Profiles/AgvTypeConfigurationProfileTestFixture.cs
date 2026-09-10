using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Profiles;

public class AgvTypeConfigurationProfileTestFixture
{
    public const string AgvTypeIdentifier = SimulatedAgvTypeConfigurationGenerator.AgvTypeIdentifier;
    public const string Manufacturer = SimulatedAgvTypeConfigurationGenerator.Manufacturer;
    public const string SeriesName = SimulatedAgvTypeConfigurationGenerator.SeriesName;
    public const string FirstSerialNumber = SimulatedAgvTypeConfigurationGenerator.FirstSerialNumber;
    public const string SecondSerialNumber = SimulatedAgvTypeConfigurationGenerator.SecondSerialNumber;
    public const int StateUpdateIntervalMs = SimulatedAgvTypeConfigurationGenerator.StateUpdateIntervalMs;
    public const int VisualizationUpdateIntervalMs = SimulatedAgvTypeConfigurationGenerator.VisualizationUpdateIntervalMs;
    public const double BatteryDischargeRate = SimulatedAgvTypeConfigurationGenerator.BatteryDischargeRate;
    public const double BatteryChargeRate = SimulatedAgvTypeConfigurationGenerator.BatteryChargeRate;
    public const int InitialBatteryCharge = SimulatedAgvTypeConfigurationGenerator.InitialBatteryCharge;
    public const int InitialBatteryChargeDeviation = SimulatedAgvTypeConfigurationGenerator.InitialBatteryChargeDeviation;
    public const int NoOfAgvs = 2;
    public const int NoAgvs = 0;

    private int _noOfAgvs = NoOfAgvs;

    public IMapper Mapper { get; } = MappingHelper.GetMapper(new AgvTypeConfigurationProfile());

    public SimulatedAgvTypeConfiguration Configuration => SimulatedAgvTypeConfigurationGenerator.Generate(noOfAgvs: _noOfAgvs);

    public AgvTypeConfigurationProfileTestFixture WithNoOfAgvs(int noOfAgvs)
    {
        _noOfAgvs = noOfAgvs;
        return this;
    }
}