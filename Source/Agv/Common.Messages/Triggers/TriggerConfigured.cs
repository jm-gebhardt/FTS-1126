using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when a trigger has been configured.
/// </summary>
/// <param name="Properties">The trigger properties.</param>
/// <param name="ConfigurationParameters">The configuration parameters.</param>
[PublicAPI]
public record TriggerConfigured(TriggerProperties Properties, ICollection<ConfigurationParameter> ConfigurationParameters) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the message.
    /// </summary>
    public Guid CorrelationId  => Properties.Id;
}