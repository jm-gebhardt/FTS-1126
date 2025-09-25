using Gebhardt.Acc.TransportOrder.Domain.ReadModels;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents a response to <see cref="GetFilteredActionGroups"/>.
/// </summary>
/// <param name="ActionGroups">The filtered action groups.</param>
public record GotFilteredActionGroups(ICollection<FilteredActionGroup> ActionGroups);