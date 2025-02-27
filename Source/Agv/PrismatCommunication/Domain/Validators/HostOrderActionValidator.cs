using FluentValidation;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Action = Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder.Action;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Validators;

/// <summary>
/// Validates <see cref="Action"/>
/// </summary>
public class HostOrderActionValidator : AbstractValidator<Action>
{
    /// <inheritdoc cref="HostOrderActionValidator" />
    public HostOrderActionValidator(KaeserGraphConfiguration configuration)
    {
        List<string> knownTargets = configuration.StationMapping.Keys.ToList();
        knownTargets.AddRange(configuration.PrismatIncompleteDropTargets);
        knownTargets.AddRange(configuration.PrismatIncompletePickTargets);
        RuleFor(a => a.Node)
            .Must(knownTargets.Contains)
            .WithReason(configuration.OrderValidationErrors[KaeserGraphConfiguration.ErrorCodeSourceUnknown])
            .WithErrorCode(KaeserGraphConfiguration.ErrorCodeSourceUnknown.ToString())
            .When(a => a.ActionType == ActionType.Pick);

        RuleFor(a => a.Node)
            .Must(knownTargets.Contains)
            .WithReason(configuration.OrderValidationErrors[KaeserGraphConfiguration.ErrorCodeTargetUnknown])
            .WithErrorCode(KaeserGraphConfiguration.ErrorCodeTargetUnknown.ToString())
            .When(a => a.ActionType == ActionType.Drop);
    }
}