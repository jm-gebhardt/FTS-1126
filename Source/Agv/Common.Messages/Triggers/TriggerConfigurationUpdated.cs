namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when configuration parameters of a trigger have been updated.
/// </summary>
/// <param name="TriggerId">The id of the trigger.</param>
/// <param name="UpdatedParameters">The updated parameters.</param>
public record TriggerConfigurationUpdated(Guid TriggerId, ICollection<ConfigurationParameter> UpdatedParameters);