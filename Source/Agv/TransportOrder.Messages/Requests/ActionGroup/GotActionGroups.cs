using TransportOrder.Domain.ReadModels;

namespace TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents the response to the command <see cref="GetActionGroups"/>.
/// </summary>
/// <param name="ActionGroups">The action groups returned in the response.</param>
public record GotActionGroups(IReadOnlyCollection<ActionGroupReadModel> ActionGroups);