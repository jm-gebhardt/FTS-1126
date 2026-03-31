using FluentValidation;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Validators;

/// <summary>
/// Validator for a test plan.
/// </summary>
public class TestPlanValidator : AbstractValidator<TestPlan>
{
    /// <inheritdoc cref="TestPlanValidator"/>
    public TestPlanValidator(IStationProvider stationProvider)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Test plan must have a name");
        RuleFor(x => x.Steps).NotEmpty().WithMessage(p => $"Test plan {p.Name} must have at least one step");
        RuleForEach(x => x.Steps).SetValidator(new TestPlanStepValidator(stationProvider));
        RuleFor(x => x.Iterations).GreaterThanOrEqualTo(0).When(x => x.Iterations.HasValue).WithMessage(p => $"Test plan {p.Name} iterations must be >= 0");
    }
}