using Gebhardt.Acc.Common.Messages.Triggers;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;

/// <summary>
/// Provides a command to create a trigger.
/// </summary>
/// <param name="Properties">The trigger properties.</param>
/// <param name="ConfigurationParameters">The configuration parameters.</param>
public record CreateTrigger(TriggerProperties Properties, ICollection<ConfigurationParameter> ConfigurationParameters) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the message.
    /// </summary>
    public Guid CorrelationId => Properties.Id;
}