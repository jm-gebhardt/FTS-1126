using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

/// <summary>
/// Definition of an AgvAction to create for an ActionGroup
/// </summary>
public record ActionDefinition
{
    /// <summary>
    /// Type of the action
    /// </summary>
    public ActionType Type
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Destination node Id of the action 
    /// </summary>
    public string NodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Deserialization
    /// </summary>
    [UsedImplicitly]
    public ActionDefinition()
    {
        NodeId = null!;
    }

    /// <inheritdoc cref="ActionDefinition"/>>
    /// <param name="actionType">Type of the action</param>
    /// <param name="nodeId">Destination node Id of the action </param>
    public ActionDefinition(ActionType actionType, string nodeId)
    {
        Type = actionType;
        NodeId = nodeId;
    }
}