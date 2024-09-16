namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request to update a configuration parameter of a trigger at a location.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="LocationId">The location id.</param>
/// <param name="ParameterName">The parameter name.</param>
/// <param name="NewValue">The new value.</param>
public record TryUpdateLocationConfigurationParameter(Guid TriggerId, Guid LocationId, string ParameterName, string NewValue);