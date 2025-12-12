using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// This class links an <see cref="AgvAction"/> to a specific <see cref="GraphNode"/> and maintains the position of the node via the Index.
/// </summary>
public class AgvActionNode
{
    /// <summary>
    /// Id of the AgvActionNode.
    /// </summary>
    public Guid Id
    {
        get;
        [UsedImplicitly]
        set;
    }

    /// <summary>
    /// The Index of the GraphNode in the action.
    /// </summary>
    public int Index
    {
        get;
        [UsedImplicitly]
        set;
    }

    /// <summary>
    /// Id of the AgvAction.
    /// </summary>
    public Guid AgvActionId
    {
        [UsedImplicitly]
        get;
        set;
    }

    /// <summary>
    /// The Id of the GraphNode.
    /// </summary>
    public Guid NodeId
    {
        get;
        [UsedImplicitly]
        set;
    }

    /// <summary>
    /// The linked GraphNode.
    /// </summary>
    public GraphNode Node { get; set; } = null!;
}