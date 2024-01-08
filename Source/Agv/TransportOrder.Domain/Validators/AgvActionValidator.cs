using FluentValidation;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Constants;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Validators;

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
    }
}