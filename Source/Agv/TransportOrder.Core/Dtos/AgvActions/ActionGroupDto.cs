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
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Order Id
        /// </summary>
        public string OrderId { get; init; }

        /// <summary>
        /// Assigned Agv
        /// </summary>
        public Guid? AssignedAgv { get; init; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedDate { get; init; }

        /// <summary>
        /// Start date when the ActionGroup is started
        /// </summary>
        public DateTime? StartedDate { get; init; }

        /// <summary>
        /// Completion date when the ActionGroup is completed
        /// </summary>
        public DateTime? CompletedDate { get; init; }

        /// <summary>
        /// State of the actionGroup. <see cref="ActionGroupState" />
        /// </summary>
        public ActionGroupState State { get; init; }

        /// <summary>
        /// List of actions
        /// </summary>
        public IList<AgvActionDto> Actions
        {
            get => _actions;
            init
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                // can be null because of AutoMapper
                _actions = value?.OrderBy(a => a.Sequence).ToList() ?? new List<AgvActionDto>();
            }
        }

        /// <summary>
        /// If true, then this ActionGroup was created manually, not by the system
        /// </summary>
        public bool IsManual { get; set; }

        /// <param name="correlationId"></param>
        /// <param name="orderId"></param>
        /// <param name="assignedAgv">Assigned Agv</param>
        /// <param name="createdDate">Creation date</param>
        /// <param name="startedDate">Start date</param>
        /// <param name="completedDate">Completion date</param>
        /// <param name="state">State of the actionGroup</param>
        /// <param name="actions">List of actions</param>
        /// <param name="isManual">If true, then this ActionGroup was created manually, not by the system</param>
        public ActionGroupDto(
            Guid correlationId,
            string orderId,
            Guid? assignedAgv,
            DateTime createdDate,
            DateTime? startedDate,
            DateTime? completedDate,
            ActionGroupState state,
            IList<AgvActionDto> actions,
            bool isManual
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
            IsManual = isManual;
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