using FluentValidation;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;

namespace Gebhardt.Acc.OrderTranslator.Domain.Validators;

/// <summary>
/// Validates <see cref="HostOrder"/>
/// </summary>
public class HostOrderValidator : AbstractValidator<HostOrder>
{
    /// <inheritdoc cref="HostOrderValidator"/>>
    public HostOrderValidator(IValidator<Domain.Models.HostOrder.Action> actionValidator)
    {
        RuleForEach(h => h.Actions)
            .SetValidator(actionValidator);
    }
}