using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;

/// <summary>
/// Provides a dto for action group counts by state.
/// </summary>
/// <param name="State">The action group state.</param>
/// <param name="Count">Number of actions groups in state</param>
public record AgvGroupCountByStateDto(ActionGroupState State, int Count);