using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Extensions for <see cref="GraphNode"/>
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// Extension block for <see cref="GraphNode"/>.
    /// </summary>
    extension(GraphNode node)
    {
        /// <summary>
        /// Converts a <see cref="GraphNode"/> to a <see cref="Vector3"/>
        /// </summary>
        /// <returns></returns>
        [PublicAPI]
        public Vector3 AsVector3() => new(node.X, node.Y, node.Z);

        /// <summary>
        /// Converts a <see cref="GraphNode"/> to a <see cref="Vector2"/> by projecting it onto the plane.
        /// </summary>
        /// <returns></returns>
        public Vector2 AsVector2() => new(node.X, node.Y);

        /// <summary>
        /// True if node type is Charge, Drop, Pick, PickDrop or Park.
        /// </summary>
        /// <returns>True if node type represents a station.</returns>
        public bool IsStation() => node.Type.Any(n => n.IsStation());

        /// <summary>
        /// True if the given node is a pick station.
        /// </summary>
        /// <returns>True if the node is a pick station; otherwise, false.</returns>
        public bool IsPickStation() => node.Type.Any(n => n is AgvGraph.NodeType.Pick or AgvGraph.NodeType.PickDrop);

        /// <summary>
        /// True if the given node is a drop station.
        /// </summary>
        /// <returns>True if the node is a drop station; otherwise, false.</returns>
        public bool IsDropStation() => node.Type.Any(n => n is AgvGraph.NodeType.Drop or AgvGraph.NodeType.PickDrop);

        /// <summary>
        /// True if the given node is either a pick or drop (or PickDrop) station.
        /// </summary>
        /// <returns>True if the node is a pick or drop station; otherwise, false.</returns>
        public bool IsPickOrDropStation() => node.IsPickStation() || node.IsDropStation();

        /// <summary>
        /// True if a nodes needs to be reserved.
        /// </summary>
        public bool NeedsReservation() => node.Type.Any(n => n is AgvGraph.NodeType.Charge or AgvGraph.NodeType.Park);

        /// <summary>
        /// Get the types of the node that need reservation.
        /// </summary>
        public ICollection<AgvGraph.NodeType> GetReservableNodeTypes() =>
            node.Type.Where(n => n is AgvGraph.NodeType.Charge or AgvGraph.NodeType.Park).ToList();

        /// <summary>
        /// True if a nodes is a charge node.
        /// </summary>
        public bool IsCharger() => node.Type.Any(n => n == AgvGraph.NodeType.Charge);

        /// <summary>
        /// True if a nodes is a park node.
        /// </summary>
        public bool IsParker() => node.Type.Any(n => n == AgvGraph.NodeType.Park);

        /// <summary>
        /// Get the primary type of the node.
        /// </summary>
        /// <returns></returns>
        public AgvGraph.NodeType GetPrimaryType() => node.Type.First();

        /// <summary>
        /// Get the additional types of the node.
        /// </summary>
        /// <returns></returns>
        public ICollection<AgvGraph.NodeType> GetAdditionalTypes() => node.Type.Skip(1).ToList();
    }

    /// <summary>
    /// Extension block for <see cref="AgvGraph.NodeType"/>.
    /// </summary>
    extension(AgvGraph.NodeType nodeType)
    {
        /// <summary>
        /// True if node type is Charge, Drop, Pick, PickDrop or Park.
        /// </summary>
        /// <returns>True if node type represents a station.</returns>
        private bool IsStation() => new[]
        {
            AgvGraph.NodeType.Charge, AgvGraph.NodeType.Drop, AgvGraph.NodeType.Pick, AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park, AgvGraph.NodeType.Maintenance
        }.Contains(nodeType);
    }
}
