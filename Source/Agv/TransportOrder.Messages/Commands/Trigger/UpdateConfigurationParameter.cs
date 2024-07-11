using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;

/// <summary>
/// Provides a command to update a configuration parameter value of a trigger.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="ParameterName">The parameter name.</param>
/// <param name="NewValue">The new value of the parameter.</param>
public record UpdateConfigurationParameter(Guid TriggerId, string ParameterName, string NewValue) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}