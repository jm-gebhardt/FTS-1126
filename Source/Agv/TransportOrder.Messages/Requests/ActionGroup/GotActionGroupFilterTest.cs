using Gebhardt.Acc.TransportOrder.Domain.ReadModels;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// 
/// </summary>
/// <param name="ActionGroups"></param>
public record GotFilteredActionGroups(ICollection<FilteredActionGroup> ActionGroups);