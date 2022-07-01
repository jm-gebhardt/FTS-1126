using MassTransit;


namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting the graph of an AGV.
    /// </summary><remarks>
    /// This data structure is optimized for
    /// information purpose by keeping additional information like coordinats and for
    /// routing algorithms by having information like metric.
    /// </remarks>
    [JetBrains.Annotations.PublicAPI]
    public interface IAgvGraphReceived : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        string AgvId { get; }

        /// <summary>
        /// The list of nodes in the graph.
        /// </summary>
        public Node[] Nodes { get; }

        /// <summary>
        /// The list of edges in the graph.
        /// </summary>
        public Edge[] Edges { get; }
    }

    /// <summary>
    /// Node types.
    /// <para>
    /// These types are coming from the specification.
    /// </para>
    /// </summary>
    [Flags]
    public enum NodeType : short
    {
        /// <summary>Charging station</summary>
        [JetBrains.Annotations.UsedImplicitly]
        Charge = 0x0001,

        /// <summary>General waypoint</summary>
        [JetBrains.Annotations.UsedImplicitly]
        WayPoint = 0x0002,

        /// <summary>Rotation point</summary>
        [JetBrains.Annotations.UsedImplicitly]
        Turn = 0x0004,

        /// <summary>Source or destination</summary>
        [JetBrains.Annotations.UsedImplicitly]
        Station = 0x0008,

        /// <summary>Waiting point</summary>
        [JetBrains.Annotations.UsedImplicitly]
        Park = 0x0010
    }

    /// <summary>
    /// Node representation.
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    public class Node
    {
        /// <summary>
        /// The unique ID of the node.
        /// </summary><remarks>
        /// Usually this is the (numeric) ID passed by the AGV vendor.
        /// For internal reasons it can be necessary to build/combine an internal identification.
        /// </remarks>
        public string? Id { get; set; }

        /// <summary>
        /// Node enabled/disabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Node type of the node.
        /// </summary>
        public NodeType Type { get; set; }

        /// <summary>
        /// The x coordinate of the node.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// The y coordinate of the node
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// The z coordinate of the node.
        /// <para>
        /// Set to default 0 when not needed.
        /// </para></summary>
        public float Z { get; set; }
    }

    /// <summary>
    /// Edge representation
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    public class Edge
    {
        /// <summary>
        /// The ID of the node where the edge starts.
        /// </summary>
        public string? From { get; set; }
        /// <summary>
        /// The ID of the node where the edge ends.
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// Distance/costs to travel this edge (unitless quantifier).
        /// </summary>
        public float Metric { get; set; }

        /// <summary>
        /// When true there is a sibling edge with <see cref="From"/> and <see cref="To"/> swapped.
        /// </summary>
        public bool Bidirectional { get; set; }

        /// <summary>
        /// The maximum allowed velocity on that edge.
        /// </summary>
        public float MaxVelocity { get; set; }
    }
}
