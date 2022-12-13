using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.States
{
    /// <summary>
    /// Action State
    /// </summary>
    public class ActionSaga : SagaStateMachineInstance
    {
        /// <inheritdoc cref="ActionSaga"/>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="action">Action of the Sage</param>
        /// <param name="currentState">Current state of the Saga</param>
        public ActionSaga(Guid correlationId,
            AgvAction action,
            string? currentState = null)
        {
            CorrelationId = correlationId;
            Action = action;
            CurrentState = currentState ?? default!;
        }

        /// <inheritdoc />
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// The Action
        /// </summary>
        public AgvAction Action { get; set; }

        /// <summary>
        /// Current state
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// The assigned Agv
        /// </summary>
        public Guid AssignedAgv { get; set; }

        /// <summary>
        /// The Id of the assigned Agv
        /// </summary>
        public string? AssignedAgvId { get; set; }
    }
}