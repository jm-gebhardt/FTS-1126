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
    public Action(Guid correlationId, Guid actionGroupId, string node, ActionType actionType, int sequenceNumber, Dictionary<InfoToAgvKey, string>? infoToAgv, DateTime? finishedDate = null)
    {
        CorrelationId = correlationId;
        ActionGroupId = actionGroupId;
        Node = node;
        ActionType = actionType;
        SequenceNumber = sequenceNumber;
        InfoToAgv = infoToAgv;
        FinishedDate = finishedDate;
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

    /// <summary>
    /// True, if the action is registered for a Doppelspiel
    /// </summary>
    public bool IsRegisteredForDoppelspiel { get; private set; }

    /// <summary>
    /// The correlation id of the Doppelspiel partner order, if any.
    /// </summary>
    public Guid? DoppelspielPartner { get; private set; }

    /// <summary>
    /// Register the action for Doppelspiel
    /// </summary>
    public void RegisterForDoppelspiel()
    {
        IsRegisteredForDoppelspiel = true;
    }

    /// <summary>
    /// Set a Doppelspiel partner.
    /// </summary>
    public void SetDoppelspielPartner(Guid doppelspielPartner)
    {
        IsRegisteredForDoppelspiel = true;
        DoppelspielPartner = doppelspielPartner;
    }

    /// <summary>
    /// Remove the Doppelspiel association.
    /// </summary>
    public void ResetDoppelspielPartner()
    {
        DoppelspielPartner = null;
    }

    /// <summary>
    /// Unregister from Doppelspiel.
    /// </summary>
    public void UnregisterFromDoppelspiel()
    {
        IsRegisteredForDoppelspiel = false;
        DoppelspielPartner = null;
    }
}