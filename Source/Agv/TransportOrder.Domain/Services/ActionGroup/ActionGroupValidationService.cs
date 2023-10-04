using FluentValidation;
using FluentValidation.Results;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc />
public class ActionGroupValidationService : IActionGroupValidationService
{
    private readonly IValidator<Models.ActionGroup.ActionGroup> _actionGroupValidator;

    /// <inheritdoc cref="ActionGroupValidationService"/>
    public ActionGroupValidationService(IValidator<Models.ActionGroup.ActionGroup> actionGroupValidator)
    {
        _actionGroupValidator = actionGroupValidator;
    }

    /// <inheritdoc />
    public async Task<List<ValidationFailure>> ValidateAsync(Models.ActionGroup.ActionGroup actionGroup)
    {
        var validationResult = await _actionGroupValidator.ValidateAsync(actionGroup);

        if (validationResult.IsValid || !validationResult.Errors.Any())
        {
            return new List<ValidationFailure>();
        }

        return validationResult.Errors;
    }
}