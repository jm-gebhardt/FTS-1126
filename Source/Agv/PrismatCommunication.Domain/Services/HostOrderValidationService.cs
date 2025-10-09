using FluentValidation;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Services;

/// <inheritdoc cref="IHostOrderValidationService"/>>
[UsedImplicitly]
public class HostOrderValidationService(IValidator<HostOrder> orderValidator, ILogger<HostOrderValidationService> logger): IHostOrderValidationService
{
    /// <inheritdoc cref="IHostOrderValidationService"/>>
    public async Task<(int ErrorCode, string ErrorInfo)?> ValidateAsync(HostOrder hostOrder)
    {
        logger.LogDebug("Validating host order {ActionGroupCorrelationId}", hostOrder.CorrelationId);
        var result = await orderValidator.ValidateAsync(hostOrder);
        if (result.IsValid)
        {
            return null;
        }
        logger.LogError("Validation of host order  {ActionGroupCorrelationId} failed", hostOrder.CorrelationId);
        var error = result.Errors.First();
        return (int.Parse(error.ErrorCode), error.ErrorMessage);
    }
}