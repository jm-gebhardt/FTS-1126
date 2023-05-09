using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions
{
    /// <summary>
    /// Consumer for action messages
    /// </summary>
    [UsedImplicitly]
    public class ActionConsumer :
        IConsumer<QueueActionGroup>,
        IConsumer<ActionCompletedEvent>,
        IConsumer<ChargingAgvsCanHandleActionsAgainEvent>,
        IConsumer<AgvBecameIdle>,
        IConsumer<AgvNeedsCharging>
    {
        private readonly ILogger<ActionConsumer> _logger;
        private readonly IActionManager _actionManager;
        private readonly IAgvRegistry _agvRegistry;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="actionManager"></param>
        /// <param name="agvRegistry"></param>
        public ActionConsumer(ILogger<ActionConsumer> logger, IActionManager actionManager, IAgvRegistry agvRegistry)
        {
            _logger = logger;
            _actionManager = actionManager;
            _agvRegistry = agvRegistry;
        }

        /// <inheritdoc/>
        public Task Consume(ConsumeContext<AgvBecameIdle> context)
        {
            var agv = _agvRegistry.GetRequiredAgv(context.Message.CorrelationId);
            return _actionManager.AssignExclusivelyAsync(agv, false, context.CancellationToken);
        }

        /// <summary>
        /// Handle <see cref="ActionGroup"/> incoming from HostOrderStateMachine
        /// </summary>
        /// <param name="context"></param>
        public Task Consume(ConsumeContext<QueueActionGroup> context)
        {
            _logger.LogInformation("Queue ActionGroup {CorrelationId}", context.Message.CorrelationId);
            return _actionManager.AddActionGroupAsync(context.Message.ActionGroup, context.CancellationToken);
        }
        
        /// <summary>
        /// Handle <see cref="ActionCompletedEvent"/> from DeviceCommunication
        /// </summary>
        public Task Consume(ConsumeContext<ActionCompletedEvent> context)
        {
            var actionCorrelationId = context.Message.CorrelationId;
            _logger.LogDebug("{MessageType} for actionId: {ActionCorrelationId}", nameof(ActionCompletedEvent), actionCorrelationId);
            return _actionManager.CompleteActionAsync(actionCorrelationId, context.CancellationToken);
        }

        /// <summary>
        /// Handle <see cref="ChargingAgvsCanHandleActionsAgainEvent"/> incoming from ChargingService
        /// </summary>
        /// <param name="context"></param>
        public Task Consume(ConsumeContext<ChargingAgvsCanHandleActionsAgainEvent> context)
        {
            return _actionManager.CheckChargingAgvs(context.CancellationToken);
        }

        /// <summary>
        /// Assign new action to get to charger soon
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Consume(ConsumeContext<AgvNeedsCharging> context)
        {
            var agv = _agvRegistry.GetRequiredAgv(context.Message.CorrelationId);
            return _actionManager.AssignExclusivelyAsync(agv, false, context.CancellationToken);
        }
    }
}