using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when configuration parameters of a trigger have been updated.
/// </summary>
/// <param name="TriggerId">The id of the trigger.</param>
/// <param name="UpdatedParameters">The updated parameters.</param>
public record TriggerConfigurationUpdated(Guid TriggerId, ICollection<ConfigurationParameter> UpdatedParameters) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;

    /// <summary>
    /// Deconstruct the message.
    /// </summary>
    /// <param name="id">The trigger id.</param>
    /// <param name="parameters">The updated configuration parameters.</param>
    public void Deconstruct(out Guid id, out ICollection<ConfigurationParameter> parameters)
    {
        parameters = UpdatedParameters;
        id = TriggerId;
    }
}