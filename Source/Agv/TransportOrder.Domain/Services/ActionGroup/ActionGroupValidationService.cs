using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc />
public class ActionGroupValidationService : IActionGroupValidationService
{
    private readonly IValidator<Models.ActionGroup.ActionGroup> _actionGroupValidator;
    private readonly ILogger<ActionGroupValidationService> _logger;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupValidationService"/> instance.
    /// </summary>
    public ActionGroupValidationService(IValidator<Models.ActionGroup.ActionGroup> actionGroupValidator, ILogger<ActionGroupValidationService> logger)
    {
        _actionGroupValidator = actionGroupValidator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(Models.ActionGroup.ActionGroup actionGroup)
    {
        _logger.LogDebug("Validating action group for order {OrderId}", actionGroup.OrderId);
        var validationResult = await _actionGroupValidator.ValidateAsync(actionGroup);

        if (!validationResult.IsValid && validationResult.Errors.Any())
        {
            _logger.LogWarning("Action group for order {OrderId} is invalid", actionGroup.OrderId);
            foreach (var error in validationResult.Errors)
            {
                _logger.LogWarning("{Message}", error.ErrorMessage);
            }
        }

        return validationResult;
    }
}