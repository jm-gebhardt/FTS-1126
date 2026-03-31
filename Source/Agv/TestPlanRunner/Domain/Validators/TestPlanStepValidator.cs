using FluentValidation;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Validators;

/// <summary>
/// Validator for a single test plan step.
/// </summary>
public class TestPlanStepValidator : AbstractValidator<TestPlanStep>
{
    /// <inheritdoc cref="TestPlanStepValidator"/>
    public TestPlanStepValidator(IStationProvider stationProvider)
    {
        RuleFor(x => x.StepType).NotNull().WithMessage(s => $"Unknown step type {s.StepType}");
        RuleFor(x => x.WaitTimeSeconds).GreaterThanOrEqualTo(0).WithMessage(s => $"Wait time must be >= 0 : {s.WaitTimeSeconds}");

        When(x => x.StepType == StepType.HostOrder, () =>
        {
            RuleFor(x => x.TargetNode).Empty().WithMessage("HostOrder step must not have a target node");
            RuleFor(x => x.Source).NotEmpty().WithMessage("HostOrder step must have a source");
            RuleFor(x => x.Source).Must( x=> stationProvider.StationExists(x ?? string.Empty)).WithMessage(s => $"HostOrder source {s.Source} does not exist");
            RuleFor(x => x.Destination).NotEmpty().WithMessage("HostOrder step must have a destination");
            RuleFor(x => x.Destination).Must(x => stationProvider.StationExists(x ?? string.Empty)).WithMessage(s => $"HostOrder destination {s.Destination} does not exist");
        }).Otherwise(() =>
        {
            RuleFor(x => x.TargetNode).NotEmpty().WithMessage(s => $"Step must have a target node ({s.TargetNode})");
            RuleFor(x => x.TargetNode).Must(x => stationProvider.StationExists(x ?? string.Empty)).WithMessage(s => $"Step target {s.TargetNode} does not exist");
            RuleFor(x => x.Source).Empty().WithMessage("Step must not have a source");
            RuleFor(x => x.Destination).Empty().WithMessage("Step must not have a destination");
            RuleFor(x => x.LeRotation).Empty().WithMessage("Step must not have a le rotation");
        });
    }
}