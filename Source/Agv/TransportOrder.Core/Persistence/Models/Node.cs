using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvGraph;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models
{
    /// <summary>
    /// Database entity "Node"
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
        [Key, Column(TypeName = "nvarchar(255)")]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Node state (see <see cref="NodeState"/>)
        /// </summary>
        [Column(TypeName = "nvarchar(255)")]
        public NodeState State { get; set; }

        /// <summary>
        /// Node type of the node.
        /// </summary>
        [Column(TypeName = "nvarchar(255)")]
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
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// The theta coordinate of the node.
        /// </summary>
        public float Theta { get; set; }

        /// <summary/>
        public ICollection<Edge> EdgesFrom { get; set; } = null!;

        /// <summary/>
        public ICollection<Edge> EdgesTo { get; set; } = null!;
    }
}