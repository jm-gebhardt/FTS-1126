namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request to reset a configuration parameter value of a trigger at a location to the default value.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="LocationId">The id of the location.</param>
/// <param name="ParameterName">The parameter name.</param>
public record TryResetLocationConfigurationParameter(Guid TriggerId, Guid LocationId, string ParameterName);