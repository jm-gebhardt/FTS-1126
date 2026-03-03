using FluentValidation;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Validators;

/// <summary>
/// Validator for a test plan.
/// </summary>
public class TestPlanValidator : AbstractValidator<TestPlan>
{
    /// <inheritdoc cref="TestPlanValidator"/>
    public TestPlanValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Steps).NotEmpty();
        RuleForEach(x => x.Steps).SetValidator(new TestPlanStepValidator());
        RuleFor(x => x.Iterations).GreaterThanOrEqualTo(0).When(x => x.Iterations.HasValue);
    }
}