using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;

/// <summary>
/// Represents the response to the <see cref="GetAgvsInstantActionsDefinitions"/> request.
/// </summary>
/// <param name="InstantActionDefinitions">The instant action definitions for the agvs.</param>
public record GotAgvsInstantActionDefinitions(Dictionary<Guid, List<InstantActionDefinition>> InstantActionDefinitions);