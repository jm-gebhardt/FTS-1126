using FluentValidation.Results;

namespace TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Implements a service to validate <see cref="Models.ActionGroup.ActionGroup"/>.
/// </summary>
public interface IActionGroupValidationService
{
    /// <summary>
    /// Validates the <see cref="Models.ActionGroup.ActionGroup"/>.
    /// </summary>
    /// <param name="actionGroup">The <see cref="Models.ActionGroup.ActionGroup"/> to validate.</param>
    /// <returns></returns>
    Task<List<ValidationFailure>> ValidateAsync(Models.ActionGroup.ActionGroup actionGroup);
}