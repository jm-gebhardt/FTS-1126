using FluentValidation;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Validators;

/// <summary>
/// Validates <see cref="HostOrder"/>
/// </summary>
public class HostOrderValidator: AbstractValidator<HostOrder>
{
    /// <inheritdoc cref="HostOrderValidator"/>>
    public HostOrderValidator(IValidator<Models.HostOrder.Action> actionValidator)
    {
        RuleForEach(h => h.Actions)
            .SetValidator(actionValidator);
    }
}