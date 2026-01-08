using FluentValidation;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.OrderTranslator.Domain.Services;

/// <inheritdoc cref="IHostOrderValidationService"/>>
[UsedImplicitly]
public class HostOrderValidationService(IValidator<HostOrder> orderValidator, ILogger<HostOrderValidationService> logger): IHostOrderValidationService
{
    /// <inheritdoc cref="IHostOrderValidationService"/>>
    public async Task<ValidationError?> ValidateAsync(HostOrder hostOrder)
    {
        logger.LogDebug("Validating host order {ActionGroupCorrelationId}", hostOrder.CorrelationId);
        var result = await orderValidator.ValidateAsync(hostOrder);
        if (result.IsValid)
        {
            return null;
        }
        logger.LogError("Validation of host order  {ActionGroupCorrelationId} failed", hostOrder.CorrelationId);
        var error = result.Errors.First();
        return Enum.Parse<ValidationError>(error.ErrorCode);
    }
}