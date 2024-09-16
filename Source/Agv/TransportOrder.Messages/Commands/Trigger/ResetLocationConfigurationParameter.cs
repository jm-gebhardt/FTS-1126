using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;

/// <summary>
/// Provides a command to reset a configuration parameter value of a trigger at a location to the default value.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="LocationId">The id of the location.</param>
/// <param name="ParameterName">The parameter name.</param>
public record ResetLocationConfigurationParameter(Guid TriggerId, Guid LocationId, string ParameterName) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}