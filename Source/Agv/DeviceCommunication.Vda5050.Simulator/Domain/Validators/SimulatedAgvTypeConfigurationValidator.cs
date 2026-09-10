using FluentValidation;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Validators;

/// <summary>
/// Validates a <see cref="SimulatedAgvTypeConfiguration"/>.
/// </summary>
[PublicAPI]
public class SimulatedAgvTypeConfigurationValidator : AbstractValidator<SimulatedAgvTypeConfiguration>
{
    /// <inheritdoc cref="SimulatedAgvTypeConfigurationValidator"/>
    public SimulatedAgvTypeConfigurationValidator()
    {
        RuleFor(configuration => configuration)
            .Must(configuration => !string.IsNullOrWhiteSpace(configuration.Manufacturer) && !string.IsNullOrWhiteSpace(configuration.SeriesName))
            .WithMessage(configuration => $"The AGV type identifier '{configuration.AgvTypeIdentifier}' is not in the form manufacturer.seriesName.");

        RuleFor(configuration => configuration.NoOfAgvs)
            .InclusiveBetween(0, SimulationLimits.MaxAgvsPerType)
            .WithMessage("The number of simulated AGVs must be between {From} and {To} but was {PropertyValue}.");

        RuleFor(configuration => configuration.StateUpdateIntervalMs)
            .GreaterThanOrEqualTo(SimulationLimits.MinUpdateIntervalMs)
            .WithMessage("The state update interval must be at least {ComparisonValue} milliseconds but was {PropertyValue}.");

        RuleFor(configuration => configuration.VisualizationUpdateIntervalMs)
            .GreaterThanOrEqualTo(SimulationLimits.MinUpdateIntervalMs)
            .WithMessage("The visualization update interval must be at least {ComparisonValue} milliseconds but was {PropertyValue}.");

        RuleFor(configuration => configuration.AgvTypeIdentifier)
            .MaximumLength(DbStringLimits.Description)
            .WithMessage("The AGV type identifier must not be longer than {MaxLength} characters but was {TotalLength}.");

        RuleFor(configuration => configuration.InitialBatteryCharge)
            .InclusiveBetween(SimulationLimits.MinBatteryChargePercent, SimulationLimits.MaxBatteryChargePercent)
            .WithMessage("The initial battery charge must be between {From} and {To} percent but was {PropertyValue}.");

        RuleFor(configuration => configuration.InitialBatteryChargeDeviation)
            .InclusiveBetween(SimulationLimits.MinBatteryChargePercent, SimulationLimits.MaxBatteryChargePercent)
            .WithMessage("The initial battery charge deviation must be between {From} and {To} percent but was {PropertyValue}.");

        RuleFor(configuration => configuration.BatteryDischargeRate)
            .GreaterThan(0)
            .WithMessage("The battery discharge rate must be greater than {ComparisonValue} percent per second but was {PropertyValue}.");

        RuleFor(configuration => configuration.BatteryChargeRate)
            .GreaterThan(0)
            .WithMessage("The battery charge rate must be greater than {ComparisonValue} percent per second but was {PropertyValue}.");
    }
}