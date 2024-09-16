namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request to update a default configuration parameter of a trigger.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="ParameterName">The parameter name.</param>
/// <param name="NewValue">The new value.</param>
public record TryUpdateDefaultConfigurationParameter(Guid TriggerId, string ParameterName, string NewValue);