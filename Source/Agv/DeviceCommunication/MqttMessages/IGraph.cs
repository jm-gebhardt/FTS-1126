using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// Graph representing the paths on which the AGV travels
    /// <para>
    /// See "graph.schema".</para></summary>
	[JetBrains.Annotations.PublicAPI]
    [EntityName("amq.topic")]   // Specifies the RabbitMq exchange where the message is published
    [RoutingKey("agv.graph")]   // Specifies the topic where subscribers can receive the message
    public interface IGraph
    {
        /// <summary>
        /// Serial number of the AGV.
        /// </summary>
        public string SerialNumber { get; set; }
        
        /// <summary>
        /// The modification date of the graph. Timestamp in ISO8601 format.
        /// </summary>
        public DateTimeOffset ModificationDate { get; set; }

        /// <summary>
        /// A unique ID for the request
        /// </summary>
        public int RequestId { get; set; }

        /// <summary>
        /// The total amount of nodes in the graph. Must correlate with list count of nodes in
        /// following parameter.
        /// </summary>
        public int NumberOfNodes { get; set; }

        /// <summary>
        /// The total amount of edges in the graph. Must correlate with list count of edges in
        /// following parameter.
        /// </summary>
        public int NumberOfEdges { get; set; }

        /// <summary>
        /// The list of nodes in the graph
        /// </summary>
        public Node[]? Nodes { get; set; }

        /// <summary>
        /// The list of edges in the graph
        /// </summary>
        public Edge[]? Edges { get; set; }
    }

    /// <summary>
    /// Node representation
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    public class Node
    {
        /// <summary>
        /// List of attribute keys used as string interpretation in <see cref="Attribute.Key"/>.
        /// </summary>
        public enum WellKnownAttributes
        {
            /// <summary>
            /// The value of attributes with this key is the ID of the source or destination node belonging to the node this attribute belongs to.
            /// </summary>
            [JetBrains.Annotations.UsedImplicitly]
            Station
        }

        /// <summary>
        /// The unique ID of the node
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// The x coordinate of the node
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// The y coordinate of the node
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// The z coordinate of the node. For example if there is a another floor. This field is not
        /// required
        /// </summary>
        public float? Z { get; set; }

        /// <summary>
        /// The list of attributes attached to the node
        /// <para>
        /// See <see cref="WellKnownAttributes"/> for string interpretations used as <see cref="Attribute.Key"/>.
        /// </para></summary>
        public Attribute[]? Attributes { get; set; }
    }

    /// <summary>
    /// Edge representation
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    public class Edge
    {
        /// <summary>
        /// The unique ID of the edge
        /// </summary>
        public int EdgeId { get; set; }

        /// <summary>
        /// The ID of the node where the edge starts
        /// </summary>
        public int StartId { get; set; }
        /// <summary>
        /// The ID of the node where the edge ends
        /// </summary>
        public int EndId { get; set; }

        /// <summary>
        /// true/false. The edge can be traveled in both directions. If false, only from start to end
        /// is allowed.
        /// </summary>
        public bool Bidirectional { get; set; }

        /// <summary>
        /// The maximum allowed velocity on that edge
        /// </summary>
        public float MaxVelocity { get; set; }
    }

    /// <summary>
    /// Node attribute
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    public class Attribute
    {
        /// <summary>
        /// The key (name) of the attribute
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// The value of the attribute
        /// </summary>
        public string Value { get; set; } = "";
    }

}
