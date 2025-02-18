using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;

/// <summary>
/// Represents the response to the <see cref="GetAgvInstantActionDefinitions"/> request.
/// </summary>
/// <param name="AgvInstantActionDefinitions">The instant actions definitions for the agv.</param>
public record GotAgvInstantActionDefinitions(ICollection<InstantActionDefinition> AgvInstantActionDefinitions);