using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services
{
    /// <summary>
    /// Host order state machine
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public sealed class HostOrderStateMachine : MassTransitStateMachine<HostOrderSaga>
    {
        private readonly ILogger<HostOrderStateMachine> _logger;

        #region States
        /// <summary>
        /// Order has been split into actions and actions have been queued into action manager
        /// </summary>
        public State OrderQueued { get; private set; } = default!;

        /// <summary>
        /// Order has been started by any AGV
        /// </summary>
        public State Started { get; private set; } = default!;

        /// <summary>
        /// Pick action has been completed, i.e. the item has been picked
        /// </summary>
        public State Picked { get; private set; } = default!;

        /// <summary>
        /// All actions has been completed
        /// </summary>
        public State Completed { get; private set; } = default!;

        /// <summary>
        /// Order failed to process
        /// </summary>
        public State Failed { get; private set; } = default!;

        /// <summary>
        /// Order has been cancelled
        /// </summary>
        public State Cancelled { get; private set; } = default!;
        #endregion

        #region Events
        /// <summary>
        /// New order detected. This is the initial event of the HostOrderStateMachine
        /// </summary>
        public Event<CreateHostOrder> CreateHostOrderEvent { get; private set; } = default!;
        #endregion

        #region Initialization
        /// <summary>
        /// HostOrderStateMachine
        /// </summary>
        public HostOrderStateMachine(ILogger<HostOrderStateMachine> logger)
        {
            _logger = logger;

            SetSagaFactory();
            InstanceState(x => x.CurrentState);

            Initially(SetCreateHostOrderHandler());
        }

        /// <summary>
        /// Set saga factory on <see cref="CreateHostOrder"/>
        /// </summary>
        private void SetSagaFactory()
        {
            // Initial creation
            Event(() => CreateHostOrderEvent, e =>
                e.SetSagaFactory(c => new HostOrderSaga(c.Message.OrderDetails))
            );
        }
        #endregion

        #region Transitions
        /// <summary>
        /// Handler for HandleHostOrder
        /// </summary>
        /// <returns>Event Binding for HandleHostOrder</returns>
        private EventActivityBinder<HostOrderSaga, CreateHostOrder> SetCreateHostOrderHandler() =>
            When(CreateHostOrderEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(CreateHostOrder), c.Message.OrderDetails.OrderId, c.Message.OrderDetails.CorrelationId))
                .TransitionTo(Initial)
                .Then(c => c.Publish(new QueueActionGroup(GetActionGroup(c.Saga))));
        #endregion

        #region Private Methods
        private ActionGroup GetActionGroup(HostOrderSaga hostOrderSaga)
        {
            var dropActionGuid = hostOrderSaga.DropActionGuid;
            var pickActionGuid = hostOrderSaga.PickActionGuid;
            var actionGroupId = hostOrderSaga.CorrelationId;
            var actionGroup = new ActionGroup(actionGroupId, hostOrderSaga.OrderId, hostOrderSaga.OrderDetails.CreationDate);

            var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, actionGroup, hostOrderSaga.OrderDetails.Source)
            {
                Successor = dropActionGuid,
                Sequence = 0
            };
            var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, actionGroup, hostOrderSaga.OrderDetails.Destination)
            {
                Predecessor = pickActionGuid,
                Sequence = 1
            };
            actionGroup.Actions = new List<AgvAction> {pickAction, dropAction};
            
            return actionGroup;
        }
        #endregion
    }
}