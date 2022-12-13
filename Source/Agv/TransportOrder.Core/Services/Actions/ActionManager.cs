using System.Collections.Concurrent;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Requests;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions
{
    /// <summary>
    /// It handles the assignment of actions to Agvs and the status forwarding to the <see cref="OrderStateMachine"/>
    /// </summary>
    public class ActionManager :
        IConsumer<OrderQueuingCommand>,
        IConsumer<AgvBecameIdle>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ActionManager> _logger;
        private readonly IAgvRegistry _agvRegistry;
        private readonly IBus _bus;
        private readonly ConcurrentDictionary<Guid, List<AgvAction>> _agvActionsByAgvCorrelationId = new();
        private readonly IActionRegistry _actionRegistry;
        private readonly SemaphoreSlim _lockSemaphoreSlim = new(1, 1);

        /// <summary/>
        public ActionManager(
            IServiceScopeFactory scopeFactory,
            ILogger<ActionManager> logger,
            IAgvRegistry agvRegistry,
            IBus bus,
            IActionRegistry actionRegistry
            )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _agvRegistry = agvRegistry;
            _bus = bus;
            _actionRegistry = actionRegistry;
        }

        /// <inheritdoc/>
        public Task Consume(ConsumeContext<OrderQueuingCommand> context) => QueueActionsForOrderAsync(context.Message, context.CancellationToken);

        /// <inheritdoc/>
        public Task Consume(ConsumeContext<AgvBecameIdle> context) => AgvBecameIdleAsync(context.Message.CorrelationId, context.CancellationToken);

        /// <summary>
        /// Creates action for the order and triggers the assignment of pending actions
        /// </summary>
        /// <param name="request">Contains the order to queue</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task QueueActionsForOrderAsync(OrderQueuingCommand request, CancellationToken cancellationToken)
        {
            var orderCorrelationId = request.CorrelationId;

            // for now, 1 order is transferred to one transport action
            // this will in the future be split into a separate action for pick and a drop
            var transportAction = new AgvAction(Guid.NewGuid(), ActionType.Transport, request.OrderDetails);

            // create ActionStateMachine
            await SendWithConfirmation<CreateActionRequest, ActionCreationSucceededEvent>(new CreateActionRequest(transportAction), cancellationToken);

            // confirm queuing to OrderStateMachine
            await _bus.Publish(new OrderQueuedEvent(orderCorrelationId), cancellationToken);

            // trigger assignment to Agv
            await RequestAssignmentAsync(cancellationToken);
        }

        /// <summary>
        /// Triggers the assignment of pending actions
        /// </summary>
        /// <param name="correlationId">The correlationId of the Agv</param>
        /// <param name="cancellationToken"></param>
        private Task AgvBecameIdleAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(nameof(AgvBecameIdleAsync));

            if (!_agvActionsByAgvCorrelationId.ContainsKey(correlationId))
            {
                _agvActionsByAgvCorrelationId[correlationId] = new List<AgvAction>();
            }

            return RequestAssignmentAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the internal list of Agvs
        /// </summary>
        private void UpdateAgvList()
        {
            IQueryable<IAgv> agvs = _agvRegistry.GetAllAgvs();
            foreach (var agv in agvs)
            {
                if (!_agvActionsByAgvCorrelationId.ContainsKey(agv.CorrelationId))
                {
                    _agvActionsByAgvCorrelationId[agv.CorrelationId] = new List<AgvAction>();
                }
            }

            List<KeyValuePair<Guid, List<AgvAction>>> notContainedAgvs = _agvActionsByAgvCorrelationId.Where(q => !agvs.Any(p => p.CorrelationId.Equals(q.Key))).ToList();
            foreach (KeyValuePair<Guid, List<AgvAction>> notContainedAgv in notContainedAgvs)
            {
                _agvActionsByAgvCorrelationId.Remove(notContainedAgv.Key, out _);
            }
        }

        /// <summary>
        /// Checks if a idle Agv can handle any pending action
        /// </summary>
        /// <returns></returns>
        private async Task Assign(CancellationToken cancellationToken)
        {
            List<AgvAction> pendingActions = _actionRegistry.GetAllPendingActions().ToList();

            // Assign pending Orders
            foreach (var action in pendingActions)
            {
                var order = action.OrderDetails;

                await AssignActionToIdleAgv(order, action, cancellationToken);
            }
        }

        private async Task AssignActionToIdleAgv(OrderDetails order, AgvAction action, CancellationToken cancellationToken)
        {
            var correlationId = Guid.NewGuid();
            // we must manually create a scope because the ActionManager is a singleton and the IRequestClient is only scoped available
            using var scope = _scopeFactory.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<AssignAgv>>();
            _logger.LogInformation("Sending AssignAgv request and waiting for response");
            Response<AgvAssignmentSucceeded, AgvAssignmentFailed> response = await client.GetResponse<AgvAssignmentSucceeded, AgvAssignmentFailed>(
                new AssignAgv(correlationId, order),
                cancellationToken, RequestTimeout.After(s: 10));

            if (response.Message is AgvAssignmentFailed assignmentFailed)
            {
                var error = assignmentFailed.Error;

                await HandleAssignmentFailed(cancellationToken, order, error);
            }
            else if (response.Message is AgvAssignmentSucceeded agvAssignmentSucceeded)
            {
                var agvCorrelationId = agvAssignmentSucceeded.AgvCorrelationId;
                string agvId = agvAssignmentSucceeded.AgvId;

                await HandleAssignmentSucceeded(action, agvCorrelationId, agvAssignmentSucceeded, agvId, cancellationToken);
            }
            else
            {
                _logger.LogError($"Bad response: {response.Message}");
            }
        }

        private async Task HandleAssignmentFailed(CancellationToken cancellationToken, OrderDetails order, AgvAssignmentFailed.ErrorCode error)
        {
            string failedReason = $"{nameof(AgvAssignmentFailed.ErrorCode)}.{error}";

            switch (error)
            {
                case AgvAssignmentFailed.ErrorCode.NoIdleAgv:
                    _logger.LogInformation("{MessageType}/{FailedReason}: For action of order: {OrderId} no idle AGV found in AgvRegistry",
                        nameof(AgvAssignmentFailed), failedReason, order.OrderId);
                    break;

                case AgvAssignmentFailed.ErrorCode.NoRouteFound:
                    _logger.LogInformation("{MessageType}/{FailedReason}: For action of order {OrderId} unable to calculate route with available AGVs of AgvRegistry",
                        nameof(AgvAssignmentFailed), failedReason, order.OrderId);
                    await _bus.Publish(new HostOrderHandlingFailed(order.CorrelationId, order.OrderId, HostOrderHandlingFailed.ErrorCode.InvalidOrderData), cancellationToken);
                    break;

                case AgvAssignmentFailed.ErrorCode.AgvRegistryEmpty:
                    _logger.LogInformation("{MessageType}/{FailedReason}: For action of order {OrderId} no discovered AGV found in AgvRegistry",
                        nameof(AgvAssignmentFailed), failedReason, order.OrderId);
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"{nameof(AgvAssignmentFailed)}Error: {error} unknown");
            }
        }

        private async Task HandleAssignmentSucceeded(AgvAction action, Guid agvCorrelationId, AgvAssignmentSucceeded agvAssignmentSucceeded, string agvId, CancellationToken cancellationToken)
        {
            _agvActionsByAgvCorrelationId[agvCorrelationId].Add(action);
            action.Nodes = agvAssignmentSucceeded.Nodes;

            await SendWithConfirmation<AssignActionRequest, ActionAssignmentSucceededEvent>(new AssignActionRequest(action, agvCorrelationId, agvId), cancellationToken);
        }

        private Task<Response<TResponse>> SendWithConfirmation<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
            where TRequest : class
            where TResponse : class
        {
            using var scope = _scopeFactory.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<TRequest>>();
            // we must wait for the confirmation, otherwise this leads to a racing condition where the next cycle of the DoUpdate method is called and the assignment is triggered twice
            _logger.LogInformation($"Sending {typeof(TRequest).Name} and awaiting for confirmation response");

            return client.GetResponse<TResponse>(request, cancellationToken);
        }

        /// <summary>
        /// Performs the cyclic sequence if it has become necessary in the meantime
        /// </summary>
        public async Task RequestAssignmentAsync(CancellationToken cancellationToken)
        {
            await _lockSemaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                UpdateAgvList();
                await Assign(cancellationToken);
            }
            finally
            {
                _lockSemaphoreSlim.Release();
            }
        }

    }
}