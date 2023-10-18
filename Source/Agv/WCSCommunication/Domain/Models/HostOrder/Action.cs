using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Models.HostOrder;

/// <summary>
/// Represents an action.
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="ActionGroupId"></param>
/// <param name="Node"></param>
/// <param name="ActionType"></param>
/// <param name="SequenceNumber"></param>
public record Action(Guid CorrelationId, Guid ActionGroupId, string Node, ActionType ActionType, int SequenceNumber);