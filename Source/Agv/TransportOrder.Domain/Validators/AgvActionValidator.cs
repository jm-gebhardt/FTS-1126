using FluentValidation;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Domain.Validators;

/// <summary>
/// Responsible for the validation of an Agv action it it's valid
/// </summary>
public class AgvActionValidator : AbstractValidator<AgvAction>
{
    /// <inheritdoc cref="AgvActionValidator" />
    public AgvActionValidator()
    {
        RuleFor(p => p.State)
            .Equal(ActionState.Created)
            .WithReason(Reasons.ActionStateMustBeCreated, p => p.State);
        RuleFor(a => a.Target.NodeId)
            .NotNull()
            .When(a => !a.IsMoveToCoordinate);
        RuleFor(a => a.Target.Position)
            .NotNull()
            .When(a => a.IsMoveToCoordinate);
    }
}