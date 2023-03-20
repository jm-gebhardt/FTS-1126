using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Messages;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions
{
    /// <summary>
    /// Manages handling of actions and reports to <see cref="OrderStateMachine"/>
    /// </summary>
    public class ActionManager :
        IConsumer<OrderQueuingCommand>,
        IConsumer<AgvBecameReady>,
        IConsumer<ActionStartedEvent>,
        IConsumer<ActionCompletedEvent>,
        IConsumer<ActionFailedEvent>,
        IConsumer<ActionCancelledEvent>,
        IConsumer<ActionCancelFailedEvent>,
        IConsumer<ActionGroup>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ActionManager> _logger;
        private readonly IBus _bus;

        /// <summary>
        /// Map of all available AGVs with list of assigned actions (when there are any)
        /// </summary>
        private readonly IDbContextFactory<TransportOrderDbContext> _transportOrderContextFactory;
        private readonly SemaphoreSlim _lockSemaphoreSlim = new(1, 1);

        /// <summary/>
        public ActionManager(
            IServiceScopeFactory scopeFactory,
            ILogger<ActionManager> logger,
            IBus bus,
            IDbContextFactory<TransportOrderDbContext> transportOrderContextFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _bus = bus;
            _transportOrderContextFactory = transportOrderContextFactory;
        }

        /// <inheritdoc/>
        public Task Consume(ConsumeContext<OrderQueuingCommand> context) => QueueActionsForOrderAsync(context.Message, context.CancellationToken);

        /// <inheritdoc/>
        public Task Consume(ConsumeContext<AgvBecameReady> context) => AgvBecameReadyAsync(context.Message.CorrelationId, context.CancellationToken);

        /// <summary>
        /// Creates action for the order and triggers the assignment of pending actions.
        /// Signals order to change to <see cref="OrderStateMachine.OrderQueued"/> state.
        /// </summary>
        /// <param name="request">Contains the order to queue</param>
        /// <param name="cancellationToken"></param>
        private async Task QueueActionsForOrderAsync(OrderQueuingCommand request, CancellationToken cancellationToken)
        {
            var orderCorrelationId = request.CorrelationId;

            // Create ActionSagas
            using var orderActionRepo = new EfOrderActionRegistry(_transportOrderContextFactory);
            orderActionRepo.Insert(request.OrderDetails, ActionType.Pick, request.OrderDetails.CreationDate, null, request.OrderDetails.Source);
            orderActionRepo.Insert(request.OrderDetails, ActionType.Drop, request.OrderDetails.CreationDate, request.OrderDetails.Source, request.OrderDetails.Destination);
            orderActionRepo.SaveChanges();

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
        private Task AgvBecameReadyAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{MessageType} for AGV {AgvCorrelationId}", nameof(AgvBecameReady), correlationId);

            return RequestAssignmentAsync(cancellationToken);
        }

        /// <summary>
        /// Handle <see cref="ActionStartedEvent"/> from DeviceCommunication
        /// </summary>
        public Task Consume(ConsumeContext<ActionStartedEvent> context)
        {
            return HandleActionStateChange(context);
        }

        /// <summary>
        /// Handle <see cref="ActionCompletedEvent"/> from DeviceCommunication
        /// </summary>
        public Task Consume(ConsumeContext<ActionCompletedEvent> context)
        {
            return HandleActionStateChange(context);
        }

        /// <summary>
        /// Handle <see cref="ActionFailedEvent"/> from DeviceCommunication
        /// </summary>
        public Task Consume(ConsumeContext<ActionFailedEvent> context)
        {
            return HandleActionStateChange(context);
        }

        /// <summary>
        /// Handle <see cref="ActionCancelledEvent"/> from DeviceCommunication
        /// </summary>
        public Task Consume(ConsumeContext<ActionCancelledEvent> context)
        {
            return HandleActionStateChange(context);
        }

        /// <summary>
        /// Handle <see cref="ActionCancelFailedEvent"/> from DeviceCommunication
        /// </summary>
        public Task Consume(ConsumeContext<ActionCancelFailedEvent> context)
        {
            return HandleActionStateChange(context);
        }

        /// <summary>
        /// Handle status chagned events from DeviceCommunication
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Task HandleActionStateChange<TMessage>(ConsumeContext<TMessage> context) where TMessage : class, CorrelatedBy<Guid>, IActionEventParent<string>
        {
            using var orderActionRepo = new EfOrderActionRegistry(_transportOrderContextFactory);
            var action = orderActionRepo.GetActionById(context.Message.CorrelationId);
            if (action == null)
            {
                _logger.LogDebug("{MessageType} but no corresponding action found for ID {OrderActionId}",
                    nameof(TMessage), context.Message.CorrelationId);
                return Task.CompletedTask;
            }

            _logger.LogDebug("{MessageType} from AGV {AgvId} for order {OrderId} / {CorrelationId}",
                nameof(TMessage), action.AssignedAgvId, action.OrderDetails.OrderId, action.OrderCorrelationId);

            var messageType = typeof(TMessage);
            if (messageType == typeof(ActionStartedEvent))
            {
                if (action.Type == ActionType.Pick)
                {
                    return context.Publish(new TransportOrderStarted(action.OrderCorrelationId, action.OrderDetails.OrderId));
                }
            }
            else if (messageType == typeof(ActionCompletedEvent))
            {
                if (action.Type == ActionType.Pick)
                {
                    context.Publish(new TransportOrderPickCompleted(action.OrderCorrelationId, action.OrderDetails.OrderId));
                }

                action.State = OrderActionState.Completed;
                orderActionRepo.SaveChanges();
                CheckNextAction(context, orderActionRepo, action.AssignedAgv!.Value, action.OrderDetails.OrderId, action.OrderCorrelationId, context.CancellationToken)
                    .Wait();
            }
            else if (messageType == typeof(ActionFailedEvent))
            {
                context.Publish(new TransportOrderFailed(action.OrderCorrelationId, action.OrderDetails.OrderId));
            }
            else if (messageType == typeof(ActionCancelledEvent))
            {
                context.Publish(new TransportOrderCancelSucceeded(action.OrderCorrelationId, action.OrderDetails.OrderId));
            }
            else if (messageType == typeof(ActionCancelFailedEvent))
            {
                context.Publish(new TransportOrderCancelFailed(action.OrderCorrelationId, action.OrderDetails.OrderId));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle <see cref="ActionGroup"/> incoming from HostOrderStateMachine
        /// </summary>
        /// <param name="context"></param>
        public Task Consume(ConsumeContext<ActionGroup> context)
        {
            var message = context.Message;
            foreach (var action in message.ActionList)
            {
                _logger.LogInformation("ActionGroup {ParentId} with ActionId: [{ActionId}] has been consumed... [{Type} from {Source} to {Destination}]",
                    message.CorrelationId, action.CorrelationId, action.ActionType, action.Source, action.Destination);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Check if there are any subsequent actions for the given order.
        /// <para>
        /// Start any subsequent actions or clean all actions of this order when finished.
        /// </para></summary>
        /// <param name="endpoint"></param>
        /// <param name="orderActionRepo"></param>
        /// <param name="assignedAgv"></param>
        /// <param name="orderId"></param>
        /// <param name="orderCorrelationId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CheckNextAction(
            IPublishEndpoint endpoint,
            IOrderActionRegistry orderActionRepo,
            Guid assignedAgv,
            string orderId,
            Guid orderCorrelationId,
            CancellationToken cancellationToken)
        {
            var action = orderActionRepo.GetSubsequentAction(orderCorrelationId);
            if (action != null)
            {
                await StartAction(orderActionRepo, action, orderId, orderCorrelationId, cancellationToken);
            }
            else
            {
                orderActionRepo.DeleteOrder(orderCorrelationId);
                orderActionRepo.SaveChanges();
                await endpoint.Publish(new ReleaseAgv(assignedAgv), cancellationToken);
                await endpoint.Publish(new TransportOrderCompleted(orderCorrelationId, orderId), cancellationToken);
            }
        }

        /// <summary>
        /// Send <see cref="StartActionCommand"/> and set action to start <see cref="OrderActionState.Started"/>.
        /// </summary>
        private async Task StartAction(
            IOrderActionRegistry orderActionRepo,
            OrderAction action,
            string orderId,
            Guid orderCorrelationId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start action {ActionType} for AGV {AgvId} for order {OrderId} / {CorrelationId}",
                action.Type, action.AssignedAgvId, action.OrderDetails.OrderId, action.OrderCorrelationId);

            var agvAction = new AgvAction(
                action.Id,
                action.AssignedAgvId!,
                action.Type,
                orderId,
                orderCorrelationId,
                action.Source,
                action.Destination!)
            {
                Nodes = action.Nodes!
            };
            // Start AGV
            await _bus.Publish(new StartActionCommand(agvAction.AgvId.ToGuid(), agvAction), cancellationToken);

            action.State = OrderActionState.Started;
            orderActionRepo.SaveChanges();

            // Notify AGV state machine
            await _bus.Publish(new ActionStartedEvent((Guid)action.AssignedAgv!, orderId), cancellationToken);
        }

        /// <summary>
        /// Checks if an idle AGV can handle any pending orders
        /// </summary>
        private async Task Assign(CancellationToken cancellationToken)
        {
            using var orderActionRepo = new EfOrderActionRegistry(_transportOrderContextFactory);
            List<OrderAction> actions = orderActionRepo.GetPendingActions(true);

            var orders = actions.GroupBy(a => a.OrderCorrelationId)
                                .Select(g => new
                                {
                                    OrderCorrelationId = g.Key,
                                    OrderActions = g.Select(x => x).ToArray()
                                }).ToList();
            foreach (var order in orders)
            {
                if (order.OrderActions.Length == 0)
                {
                    _logger.LogError("There are no actions for order {CorrelationId}", order.OrderCorrelationId);
                    break;
                }

                var orderDetails = order.OrderActions[0].OrderDetails;
                // State of the first pending action decides for being assigned or started
                var state = order.OrderActions[0].State;

                if (state == OrderActionState.Created)
                {
                    _logger.LogDebug("Trying to assign order {OrderId} / {CorrelationId}", orderDetails.OrderId, order.OrderCorrelationId);

                    // We have to manually create a scope because the ActionManager is a singleton but IRequestClient is scoped
                    using var scope = _scopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<IRequestClient<AssignAgv>>();

                    _logger.LogDebug("Sending {MessageType} for order {OrderId} / {CorrelationId} request and waiting for response",
                        nameof(AssignAgv), orderDetails.OrderId, order.OrderCorrelationId);
                    Response<AgvAssignmentSucceeded, AgvAssignmentFailed> response = await client.GetResponse<AgvAssignmentSucceeded, AgvAssignmentFailed>(
                        new AssignAgv(Guid.NewGuid(), orderDetails),
                        cancellationToken, RequestTimeout.After(s: 10));

                    if (response.Message is AgvAssignmentSucceeded agvAssignmentSucceeded)
                    {
                        // Assign all actions
                        await HandleAssignmentSucceeded(orderActionRepo, order.OrderCorrelationId, order.OrderActions, agvAssignmentSucceeded, cancellationToken);

                        await StartAction(orderActionRepo, order.OrderActions[0], orderDetails.OrderId, orderDetails.CorrelationId,
                                          cancellationToken);
                    }
                    else if (response.Message is AgvAssignmentFailed assignmentFailed)
                    {
                        await HandleAssignmentFailed(orderDetails, assignmentFailed.Error, cancellationToken);
                    }
                    else
                    {
                        _logger.LogError("Unexpected response for {MessageType}: {Response}", nameof(AssignAgv), response.Message);
                    }
                }

                else // Assigned
                {
                    if (orderActionRepo.GetActionsOfOrder(order.OrderCorrelationId).All(c => c.State != OrderActionState.Started))
                    {
                        // There is no started action
                        _logger.LogDebug("Respawning actions for order {OrderId} / {CorrelationId}", orderDetails.OrderId, order.OrderCorrelationId);

                        await StartAction(orderActionRepo, order.OrderActions[0], orderDetails.OrderId, orderDetails.CorrelationId,
                                          cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Handle successful assignment of an order.
        /// <para>
        /// Set actions assigned and send <see cref="OrderAssignmentSucceeded"/> to <see cref="OrderStateMachine"/>.
        /// </para></summary>
        /// <param name="orderActionRepo">Order action repository</param>
        /// <param name="orderCorrelationId">Order correlation ID</param>
        /// <param name="actions">Complete list of actions belonging to that order (make sure list is not empty)</param>
        /// <param name="agvAssignmentSucceeded">Success message containing AGV information</param>
        /// <param name="cancellationToken"></param>
        private async Task HandleAssignmentSucceeded(
            IOrderActionRegistry orderActionRepo,
            Guid orderCorrelationId,
            OrderAction[] actions,
            AgvAssignmentSucceeded agvAssignmentSucceeded,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("{MessageType} for order {OrderId} / {CorrelationId}", nameof(AgvAssignmentSucceeded), actions[0].OrderDetails.OrderId, orderCorrelationId);

            try
            {
                foreach (var action in actions)
                {
                    action.AssignedAgv = agvAssignmentSucceeded.AgvCorrelationId;
                    action.AssignedAgvId = agvAssignmentSucceeded.AgvId;
                    action.State = OrderActionState.Assigned;
                    action.Nodes = RoutingHelper.CutNodes(agvAssignmentSucceeded.Nodes, action.Source, action.Destination);
                }
                orderActionRepo.SaveChanges();

                await _bus.Publish(new OrderAssignmentSucceeded(orderCorrelationId, agvAssignmentSucceeded.AgvId, agvAssignmentSucceeded.AgvCorrelationId),
                                   cancellationToken);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("Failed to assign action to AGV. {Exception}", ex);
            }
        }

        /// <summary>
        /// Handle failed assignment of an order
        /// </summary>
        /// <param name="order">Order details</param>
        /// <param name="error">Error message containing failure reason</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private async Task HandleAssignmentFailed(OrderDetails order, AgvAssignmentFailed.ErrorCode error, CancellationToken cancellationToken)
        {
            string failedReason = $"{nameof(AgvAssignmentFailed.ErrorCode)}.{error}";

            switch (error)
            {
                case AgvAssignmentFailed.ErrorCode.NoReadyAgv:
                    _logger.LogInformation("{MessageType}/{FailedReason}: For action of order: {OrderId} no ready AGV found in AgvRegistry",
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

        /// <summary>
        /// Assign pending orders to AGV
        /// </summary><remarks>
        /// This is called when a new order is queued, when an AGV became idle or cyclically.
        /// Triggered by order is not prioritized to let the assign algorithm decide which order should be assigned next.
        /// </remarks>
        public async Task RequestAssignmentAsync(CancellationToken cancellationToken)
        {
            await _lockSemaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                await Assign(cancellationToken);
            }
            finally
            {
                _lockSemaphoreSlim.Release();
            }
        }
    }
}