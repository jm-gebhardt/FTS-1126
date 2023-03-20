using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Simulate finishing of host orders
    /// </summary>
    public class AgvStateConsumer : IConsumer<MqttState>
    {
        private readonly ILogger<AgvStateConsumer> _logger;
        private readonly IAgvRegistry _agvRegistry;

        /// <summary>
        /// Create consumer for agv status message (<see cref="MqttState"/>)
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        /// <param name="agvRegistry"></param>
        [PublicAPI]
        public AgvStateConsumer(ILogger<AgvStateConsumer> logger, IAgvRegistry agvRegistry)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
        }

        /// <summary>
        /// Translate to AGV status message to AgvStatusReceived message
        /// </summary>
        public async Task Consume(ConsumeContext<MqttState> context)
        {
            var message = context.Message;
            string serialNumber = GetSerialNumber(message);

            if (string.IsNullOrEmpty(serialNumber))
            {
                _logger.LogError("Missing serialNumber in {MessageType} message: {Status}", nameof(MqttState), context.Message);
                throw new InvalidOperationException($"Cannot handle {typeof(MqttState)} message, serialNumber must not be null or empty");
            }
            _logger.LogDebug("{MessageType} from {AgvId} received", nameof(MqttState), serialNumber);

            var agvStatus = new AgvStatus(
                    serialNumber.ToGuid(),
                    GetOrderId(message),
                    GetBusy(message),
                    GetAutomatic(message),
                    GetBatteryCharge(message),
                    GetCharging(message),
                    GetDisturbed(message),
                    GetLastNodeId(message)
                    );

            var agvStatusReceived = new AgvStatusReceived(
                correlationId: serialNumber.ToGuid(),
                agvId: serialNumber,
                agvStatus: agvStatus
            );
            await context.Publish(agvStatusReceived, context.CancellationToken);

            if (context.Message.AgvPosition != null)
            {
                await context.Publish(new AgvPositionReceived(
                    correlationId: serialNumber.ToGuid(),
                    agvId: serialNumber,
                    agvPosition: new Common.Messages.AgvManagement.AgvPosition(
                        positionInitialized: context.Message.AgvPosition.PositionInitialized,
                        x: context.Message.AgvPosition.X,
                        y: context.Message.AgvPosition.Y * -1, // Special treatment for Karis AGV. Y Axis is inverted
                        z: 0f,
                        theta: Mathematics.ConvertToRadians(90.0f) - context.Message.AgvPosition.Theta, // Special treatment for Karis AGV. 90� in our System (AGV facing right) is 0� in our world
                        deviationRange: context.Message.AgvPosition.DeviationRange
                    )), context.CancellationToken);
            }

            FeedAgvRegistry(serialNumber.ToGuid(), agvStatusReceived);
        }

        private void FeedAgvRegistry(Guid correlationId, AgvStatusReceived agvStatusReceived)
        {
            _agvRegistry.AddOrUpdateAgvStatus(correlationId, agvStatusReceived);
        }

        private string GetSerialNumber(MqttState message)
        {
            return message.SerialNumber;
        }

        private string GetOrderId(MqttState message)
        {
            return message.OrderId;
        }

        private bool GetBusy(MqttState message)
        {
            if (message.ActionStates == null)
            {
                return false;
            }

            foreach (var actionState in message.ActionStates)
            {
                switch (actionState.ActionStatus)
                {
                    case ActionStatus.Failed: break;
                    case ActionStatus.Finished: break;
                    case ActionStatus.Initializing: return true;
                    case ActionStatus.Running: return true;
                    case ActionStatus.Waiting: break;
                }
            }

            return false;
        }

        private bool GetAutomatic(MqttState message)
        {
            switch (message.OperatingMode)
            {
                case OperatingMode.Automatic: return true;
                case OperatingMode.Manual: return false;
                case OperatingMode.Semiautomatic: return false;
                case OperatingMode.Service: return false;
                case OperatingMode.Teachin: return false;
                default: return false;
            }
        }

        private int GetBatteryCharge(MqttState message)
        {
            if (message.BatteryState == null)
            {
                return 0;
            }

            return (int)(message.BatteryState.BatteryCharge + 0.5);
        }

        private bool GetCharging(MqttState message)
        {
            if (message.BatteryState == null)
            {
                return false;
            }

            return message.BatteryState.Charging;
        }

        private bool GetDisturbed(MqttState message)
        {
            if (message.SafetyState != null)
            {
                if (message.SafetyState.EStop != EStop.None)
                {
                    return true;
                }

                if (message.SafetyState.FieldViolation)
                {
                    return true;
                }
            }

            return false;
        }

        private string? GetLastNodeId(MqttState message)
        {
            if (!string.IsNullOrEmpty(message.LastNodeId))
            {
                return message.LastNodeId;
            }
            else
            {
                return null;
            }
        }
    }
}
