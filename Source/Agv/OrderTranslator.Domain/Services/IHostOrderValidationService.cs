using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;

namespace Gebhardt.Acc.OrderTranslator.Domain.Services;

/// <summary>
/// Provides validations.
/// </summary>
public interface IHostOrderValidationService
{
    /// <summary>
    /// Validates a host order.
    /// </summary>
    /// <param name="hostOrder">The host order to validate.</param>
    /// <returns>The validationError.</returns>
    Task<ValidationError?> ValidateAsync(HostOrder hostOrder);
}