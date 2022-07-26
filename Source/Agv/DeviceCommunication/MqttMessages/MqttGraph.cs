using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// Graph representing the paths on which the AGV travels.
    /// <para>See "graph.schema".</para>
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    [EntityName("amq.topic")]   // Specifies the RabbitMq exchange where the message is published
    [RoutingKey("agv.graph")]   // Specifies the topic where subscribers can receive the message
    public record MqttGraph
    {
        /// <summary>
        /// Serial number of the AGV.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// The modification date of the graph. Timestamp in ISO8601 format.
        /// </summary>
        public DateTimeOffset ModificationDate { get; }

        /// <summary>
        /// The unique ID replied from the request.
        /// </summary>
        public Guid? RequestId { get; set; }

        /// <summary>
        /// The total amount of nodes in the graph. Must correlate with list count of nodes in
        /// following parameter.
        /// </summary>
        public int NumberOfNodes { get; }

        /// <summary>
        /// The total amount of edges in the graph. Must correlate with list count of edges in
        /// following parameter.
        /// </summary>
        public int NumberOfEdges { get; }

        /// <summary>
        /// The list of nodes in the graph
        /// </summary>
        public Node[]? Nodes { get; }

        /// <summary>
        /// The list of edges in the graph
        /// </summary>
        public Edge[]? Edges { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqttGraph"/>
        /// </summary>
        public MqttGraph(
            string serialNumber,
            DateTimeOffset modificationDate,
            int numberOfNodes,
            int numberOfEdges,
            Node[] nodes,
            Edge[] edges)
        {
            SerialNumber = serialNumber;
            ModificationDate = modificationDate;
            NumberOfNodes = numberOfNodes;
            NumberOfEdges = numberOfEdges;
            Nodes = nodes;
            Edges = edges;
        }
    }

    /// <summary>
    /// Node representation
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    public record Node
    {
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
        /// The z coordinate of the node (e.g. if there is a another floor).
        /// This field is not required.
        /// </summary>
        public float? Z { get; set; }
    }

    /// <summary>
    /// Edge representation
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    public record Edge
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
    public record Attribute
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
