using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;
using CommonAgvGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Extension methods for inferring common graph node types from domain AGV actions.
/// </summary>
[PublicAPI]
public static class AgvNodeActionExtensions
{
    extension(ICollection<AgvNodeAction> actions)
    {
        /// <summary>
        /// Infers the common graph <see cref="CommonAgvGraph.NodeType"/> from the action types
        /// defined on a node (e.g. pick, drop).
        /// </summary>
        /// <returns>
        /// A single <see cref="CommonAgvGraph.NodeType"/>,
        /// a <see cref="List{T}"/> of node types when multiple apply.
        /// </returns>
        public object InferNodeType()
        {
            HashSet<string> actionTypes = actions.Select(a => a.ActionType).ToHashSet();

            bool hasPick = actionTypes.Contains(Vda5050Specifications.ActionTypes.Pick);
            bool hasDrop = actionTypes.Contains(Vda5050Specifications.ActionTypes.Drop);
            bool hasCharge = actionTypes.Contains(Vda5050Specifications.ActionTypes.StartCharging);

            var nodeTypes = new List<CommonAgvGraph.NodeType>();

            if (hasPick && hasDrop)
            {
                nodeTypes.Add(CommonAgvGraph.NodeType.PickDrop);
            }
            else if (hasPick)
            {
                nodeTypes.Add(CommonAgvGraph.NodeType.Pick);
            }
            else if (hasDrop)
            {
                nodeTypes.Add(CommonAgvGraph.NodeType.Drop);
            }

            if (hasCharge)
            {
                nodeTypes.Add(CommonAgvGraph.NodeType.Charge);
            }

            if (nodeTypes.Count == 0)
            {
                return CommonAgvGraph.NodeType.WayPoint;
            }

            return nodeTypes.Count == 1 ? nodeTypes[0] : nodeTypes;
        }
    }
}