using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models
{
    /// <summary>
    /// State of an AGV action
    /// </summary>
    public enum OrderActionState
    {
        /// <summary>
        /// Action created
        /// </summary>
        Created,

        /// <summary>
        /// Action assigned
        /// </summary>
        Assigned,

        /// <summary>
        /// Action transmitted to DeviceCommunication
        /// </summary>
        Started,

        /// <summary>
        /// Action completed
        /// </summary>
        Completed
    }

    /// <summary>
    /// Database entity "AgvAction"
    /// </summary>
    [Index(nameof(OrderCorrelationId), nameof(SequenceNumber), IsUnique = true)]
    public class OrderAction
    {
        /// <summary>
        /// Unique ID (primary key)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; init; }

        /// <summary>
        /// Order correlation
        /// </summary>
        public Guid OrderCorrelationId { get; init; }

        /// <summary>
        /// Sequence number of actions belonging to an order in ascending order
        /// </summary>
        public int SequenceNumber { get; init; }

        /// <summary>
        /// See <see cref="ActionType"/>
        /// </summary>
        [Column(TypeName = "nvarchar(255)")]
        public ActionType Type { get; set; }

        /// <summary>
        /// Order details
        /// </summary>
        public OrderDetails OrderDetails { get; init; }

        /// <summary>
        /// See <see cref="OrderActionState"/>
        /// </summary>
        [Column(TypeName = "nvarchar(255)")]
        public OrderActionState State { get; set; }

        /// <summary>
        /// Assigned AGV (null when unassigned)
        /// </summary>
        public Guid? AssignedAgv { get; set; }

        /// <summary>
        /// AGV ID of the assigned AGV (for diagnostic purpose)
        /// </summary>
        public string? AssignedAgvId { get; set; }

        /// <summary>
        /// Source of the trip, null when unknown (last node ID will be assumed)
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Destination of the trip
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// The nodes of the trip
        /// </summary>
        public string[]? Nodes { get; set; }

        /// <summary>
        /// Creation DateTime
        /// </summary>
        public DateTime? OrderCreationDateTime { get; set; }
        
        /// <summary>
        /// Constructor
        /// <para>
        /// The action state automatically is set to <see cref="OrderActionState.Created"/>.
        /// </para></summary>
        /// <param name="order">Order details</param>
        /// <param name="sequenceNumber">Sequence number of the actions within one order in ascending order</param>
        /// <param name="type">Action type</param>
        /// <param name="source">Source node or null for last node ID</param>
        /// <param name="destination">Destination node</param>
        /// <param name="orderCreationDateTime"></param>
        /// <returns>AGV action instance</returns>
        public OrderAction(OrderDetails order, int sequenceNumber, ActionType type, string? source = null, string? destination = null, DateTime? orderCreationDateTime = null)
        {
            OrderCorrelationId = order.CorrelationId;
            SequenceNumber = sequenceNumber;
            Type = type;
            OrderDetails = order;
            State = OrderActionState.Created;
            Source = source;
            Destination = destination;
            OrderCreationDateTime = orderCreationDateTime;
        }

        /// <summary>
        /// Just to satisfy Entity Framework
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        private OrderAction()
        {
            OrderDetails = new OrderDetails(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, HostOrderCreator.Simulation);
        }
    }
}
