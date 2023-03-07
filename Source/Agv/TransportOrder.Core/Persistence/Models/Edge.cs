using System.ComponentModel.DataAnnotations.Schema;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models
{
    /// <summary>
    /// Database entity "Edge"
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class Edge
    {
        /// <summary/>
        public Node From { get; set; } = null!;

        /// <summary/>
        public Node To { get; set; } = null!;

        /// <summary/>
        [Column(Order = 0, TypeName = "nvarchar(255)")]
        public string FromId { get; set; } = null!;

        /// <summary/>
        [Column(Order = 1, TypeName = "nvarchar(255)")]
        public string ToId { get; set; } = null!;

        /// <summary>
        /// Distance/costs to travel this edge (unitless quantifier).
        /// </summary>
        public float Metric { get; set; }

        /// <summary>
        /// If True, this Edge cannot be used
        /// </summary>
        public bool IsBlocked { get; set; }
    }
}