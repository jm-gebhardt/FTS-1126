using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

/// <summary>
/// Represents an action.
/// </summary>
[PublicAPI]
public record Action
{
    /// <inheritdoc cref="Action"/>>
    public Action(Guid CorrelationId, Guid ActionGroupId, string Node, ActionType ActionType, int SequenceNumber, Dictionary<InfoToAgvKey, string>? InfoToAgv, DateTime? FinishedDate = null)
    {
        this.CorrelationId = CorrelationId;
        this.ActionGroupId = ActionGroupId;
        this.Node = Node;
        this.ActionType = ActionType;
        this.SequenceNumber = SequenceNumber;
        this.InfoToAgv = InfoToAgv;
        this.FinishedDate = FinishedDate;
    }

    /// <summary>
    /// Finish action.
    /// </summary>
    /// <param name="finishedDate"></param>
    public void Finish(DateTime finishedDate)
    {
        if (FinishedDate != null)
        {
            return;
        }
        FinishedDate = finishedDate;
    }

    /// <summary>The correlation id of the action.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>The action group correlation id.</summary>
    public Guid ActionGroupId { get; init; }

    /// <summary>The target node.</summary>
    public string Node { get; init; }

    /// <summary>The type of the action</summary>
    public ActionType ActionType { get; init; }

    /// <summary>The sequence number in the action group.</summary>
    public int SequenceNumber { get; init; }

    /// <summary>Information that will be sent to the agv.</summary>
    public Dictionary<InfoToAgvKey, string>? InfoToAgv { get; init; }
    
    /// <summary>The time the action has been finished.</summary>
    public DateTime? FinishedDate { get; private set; }
}