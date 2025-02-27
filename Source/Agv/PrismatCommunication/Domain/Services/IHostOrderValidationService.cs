using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Services;

/// <summary>
/// Provides validations.
/// </summary>
public interface IHostOrderValidationService
{
    /// <summary>
    /// Validates a host order.
    /// </summary>
    /// <param name="hostOrder">The host order to validate.</param>
    /// <returns>The error code and information if validation fails.</returns>
    Task<(int ErrorCode, string ErrorInfo)?> ValidateAsync(HostOrder hostOrder);
}