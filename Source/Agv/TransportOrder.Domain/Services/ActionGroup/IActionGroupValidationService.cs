using FluentValidation.Results;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Implements a service to validate <see cref="Models.ActionGroup.ActionGroup"/>.
/// </summary>
public interface IActionGroupValidationService
{
    /// <summary>
    /// Validates the <see cref="Models.ActionGroup.ActionGroup"/>.
    /// </summary>
    /// <param name="actionGroup">The <see cref="Models.ActionGroup.ActionGroup"/> to validate.</param>
    /// <returns>The ValidationResult of FluentValidation.</returns>
    Task<ValidationResult> ValidateAsync(Models.ActionGroup.ActionGroup actionGroup);
}