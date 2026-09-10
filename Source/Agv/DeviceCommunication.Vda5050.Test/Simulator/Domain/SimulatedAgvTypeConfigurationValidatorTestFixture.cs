using FluentValidation;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Validators;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Domain;

public class SimulatedAgvTypeConfigurationValidatorTestFixture
{
    private const string Manufacturer = SimulatedAgvTypeConfigurationGenerator.Manufacturer;
    private const string SeriesName = SimulatedAgvTypeConfigurationGenerator.SeriesName;

    public const string IdentifierWithoutSeparator = Manufacturer;
    public const string IdentifierWithTrailingSeparator = $"{Manufacturer}.";
    public const string IdentifierWithLeadingSeparator = $".{SeriesName}";
    public const string Empty = "";
    public const int NoAgvs = 0;
    public const int MaxAgvs = SimulationLimits.MaxAgvsPerType;
    public const int NegativeNoOfAgvs = -1;
    public const int TooManyAgvs = SimulationLimits.MaxAgvsPerType + 1;
    public const int MinIntervalMs = SimulationLimits.MinUpdateIntervalMs;
    public const int TooShortIntervalMs = SimulationLimits.MinUpdateIntervalMs - 1;

    public IValidator<SimulatedAgvTypeConfiguration> Sut { get; } = new SimulatedAgvTypeConfigurationValidator();
}