using FluentValidation;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Validators;

/// <summary>
/// Validator for a single test plan step.
/// </summary>
public class TestPlanStepValidator : AbstractValidator<TestPlanStep>
{
    /// <inheritdoc cref="TestPlanStepValidator"/>
    public TestPlanStepValidator()
    {
        RuleFor(x => x.StepType).NotNull();
        RuleFor(x => x.WaitTimeSeconds).GreaterThanOrEqualTo(0);

        When(x => x.StepType == StepType.HostOrder, () =>
        {
            RuleFor(x => x.TargetNode).Empty();
            RuleFor(x => x.Source).NotEmpty();
            RuleFor(x => x.Destination).NotEmpty();
        }).Otherwise(() =>
        {
            RuleFor(x => x.TargetNode).NotEmpty();
            RuleFor(x => x.Source).Empty();
            RuleFor(x => x.Destination).Empty();
            RuleFor(x => x.LeRotation).Empty();
        });
    }
}