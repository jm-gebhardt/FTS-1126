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

        /// <summary>
        /// Create consumer for agv status message (<see cref="MqttState"/>)
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        [PublicAPI]
        public AgvStateConsumer(ILogger<AgvStateConsumer> logger)
        {
            _logger = logger;
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
                _logger.LogError("Missing serialNumber in MqttState message: {Status}", context.Message);
                throw new InvalidOperationException($"Cannot handle {typeof(MqttState)} message, serialNumber must not be null or empty");
            }

            var agvStatus = new AgvStatus(
                serialNumber.ToGuid(),
                GetOrderId(message),
                GetBusy(message),
                GetAutomatic(message),
                GetBatteryCharge(message),
                GetCharging(message),
                GetDisturbed(message));

            await context.Publish(new AgvStatusReceived(serialNumber.ToGuid(), serialNumber, agvStatus), context.CancellationToken);
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
    }
}
