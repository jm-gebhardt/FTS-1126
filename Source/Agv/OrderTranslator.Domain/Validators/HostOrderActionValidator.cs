using FluentValidation;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;

namespace Gebhardt.Acc.OrderTranslator.Domain.Validators;

/// <summary>
/// Validates <see cref="Action"/>
/// </summary>
public class HostOrderActionValidator : AbstractValidator<Domain.Models.HostOrder.Action>
{
    /// <inheritdoc cref="HostOrderActionValidator" />
    public HostOrderActionValidator(Models.Station.StationConfiguration configuration)
    {
        RuleFor(a => a.Node)
            .Must(configuration.GetKnownSources().Contains)
            .WithErrorCode(nameof(ValidationError.SourceUnknown))
            .When(a => a.ActionType == ActionType.Pick);

        RuleFor(a => a.Node)
            .Must(configuration.GetKnownTargets().Contains)
            .WithErrorCode(nameof(ValidationError.TargetUnknown))
            .When(a => a.ActionType == ActionType.Drop);
    }
}