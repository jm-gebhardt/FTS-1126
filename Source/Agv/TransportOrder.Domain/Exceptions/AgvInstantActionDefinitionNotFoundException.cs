using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception raised when an agv instant action definition was not found.
/// </summary>
public class AgvInstantActionDefinitionNotFoundException : NotFoundException
{
    /// <inheritdoc cref="AgvInstantActionDefinitionNotFoundException"/>>
    public AgvInstantActionDefinitionNotFoundException(string actionType) : base($"Instant action of type {actionType}") { }

    /// <inheritdoc cref="AgvInstantActionDefinitionNotFoundException"/>>
    public AgvInstantActionDefinitionNotFoundException(string actionType, Guid agvCorrelationId) : base($"Instant action of type {actionType} for agv {agvCorrelationId}") { }
}