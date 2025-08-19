using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Messages.Factories;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Utilities;

/// <summary>
/// Provides extension methods for publishing messages using the publish endpoint.
/// </summary>
public static class PublishEndpointExtensions
{
    /// <summary>
    /// Publishes updated system AGV error messages to the event bus in case of changes in AGV errors.
    /// </summary>
    /// <param name="endpoint">The publishEndpoint.</param>
    /// <param name="agvCorrelationId">The AGV correlation ID.</param>
    /// <param name="activeErrors">The list of currently active errors.</param>
    /// <param name="updatedErrors">The list of updated errors.</param>
    /// <param name="agvPosition">The current AGV position.</param>
    public static async Task PublishSystemAgvErrorsAsync(this IPublishEndpoint endpoint, Guid agvCorrelationId, IReadOnlyList<AgvError> activeErrors, List<AgvError> updatedErrors, AgvPosition agvPosition)
    {
        var updateSystemAgvErrors = SystemAgvErrorFactory.CreateUpdateSystemAgvErrors(agvCorrelationId, activeErrors, updatedErrors, agvPosition);
        if (updateSystemAgvErrors != null)
        {
            await endpoint.Publish(updateSystemAgvErrors);
        }
    }
}