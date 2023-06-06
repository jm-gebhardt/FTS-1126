using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions
{
    /// <summary>
    /// Action Group model
    /// </summary>
    [PublicAPI]
    public record ActionGroupDto
    {
        private IList<AgvActionDto> _actions = new List<AgvActionDto>();

        /// <summary>
        /// CorrelationId of ActionGroup
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Order Id
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// Assigned Agv
        /// </summary>
        public Guid? AssignedAgv { get; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedDate { get; }

        /// <summary>
        /// Start date when the ActionGroup is started
        /// </summary>
        public DateTime? StartedDate { get; }

        /// <summary>
        /// Completion date when the ActionGroup is completed
        /// </summary>
        public DateTime? CompletedDate { get; }

        /// <summary>
        /// State of the actionGroup. <see cref="ActionGroupState" />
        /// </summary>
        public ActionGroupState State { get; }

        /// <summary>
        /// List of actions
        /// </summary>
        public IList<AgvActionDto> Actions
        {
            get => _actions;
            init => _actions = value.OrderBy(a => a.Sequence).ToList();
        }

        /// <param name="correlationId"></param>
        /// <param name="orderId"></param>
        /// <param name="assignedAgv">Assigned Agv</param>
        /// <param name="createdDate">Creation date</param>
        /// <param name="startedDate">Start date</param>
        /// <param name="completedDate">Completion date</param>
        /// <param name="state">State of the actionGroup</param>
        /// <param name="actions">List of actions</param>
        public ActionGroupDto(
            Guid correlationId,
            string orderId,
            Guid? assignedAgv,
            DateTime createdDate,
            DateTime? startedDate,
            DateTime? completedDate,
            ActionGroupState state,
            IList<AgvActionDto> actions
        )
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            AssignedAgv = assignedAgv;
            CreatedDate = createdDate;
            StartedDate = startedDate;
            CompletedDate = completedDate;
            State = state;
            Actions = actions;
        }

        /// <summary>
        /// AutoMapper ctor
        /// </summary>
        public ActionGroupDto()
        {
            OrderId = null!;
            Actions = null!;
        }
    }
}