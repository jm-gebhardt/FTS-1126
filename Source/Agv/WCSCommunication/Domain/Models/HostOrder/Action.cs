using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.WCSCommunication.Domain.Models.HostOrder;

/// <summary>
/// Represents an action.
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="ActionGroupId"></param>
/// <param name="Node"></param>
/// <param name="ActionType"></param>
/// <param name="SequenceNumber"></param>
[PublicAPI] 
public record Action(Guid CorrelationId, Guid ActionGroupId, string Node, ActionType ActionType, int SequenceNumber);