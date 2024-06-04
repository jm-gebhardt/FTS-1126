using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

/// <summary>
/// Represents an action.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action.</param>
/// <param name="ActionGroupId">The action group correlation id.</param>
/// <param name="Node">The target node.</param>
/// <param name="ActionType">The type of the action</param>
/// <param name="SequenceNumber">The sequence number in the action group.</param>
public record Action(Guid CorrelationId, Guid ActionGroupId, string Node, ActionType ActionType, int SequenceNumber);