using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Simulate finishing of host orders
    /// </summary>
    public class AgvStateConsumer : IConsumer<IState>
    {
        private readonly ILogger<AgvStateConsumer> _logger;

        /// <summary>
        /// Create consumer with message handler delegate for simulation.
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        [PublicAPI]
        public AgvStateConsumer(ILogger<AgvStateConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Translate to AGV status message to IAgvStatusReceived message
        /// </summary>
        public async Task Consume(ConsumeContext<IState> context)
        {
            var message = context.Message;

            await context.Publish<IAgvStatusReceived>(new
            {
                Agv = GetAgv(message),
                OrderId = GetOrderId(message),
                Busy = GetBusy(message),
                Automatic = GetAutomatic(message),
                BatteryCharge = GetBatteryCharge(message),
                Charging = GetCharging(message),
                Disturbed = GetDisturbed(message)
            });
        }

        private string GetAgv(IState message)
        {
            if (string.IsNullOrEmpty(message.SerialNumber))
            {
                _logger.LogWarning("Received AGV status without serial number");
                return string.Empty;
            }

            return message.SerialNumber;
        }

        private string? GetOrderId(IState message)
        {
            return message.OrderId;
        }

        private bool GetBusy(IState message)
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

        private bool GetAutomatic(IState message)
        {
            switch (message.OperatingMode)
            {
                case OperatingMode.Automatic: return true;
                case OperatingMode.Manual: return false;
                case OperatingMode.Semiautomatic: return false;
                case OperatingMode.Service: return false;
                case OperatingMode.TeachIn: return false;
                default: return false;
            }
        }

        private int GetBatteryCharge(IState message)
        {
            if (message.BatteryState == null)
            {
                return 0;
            }

            return (int)(message.BatteryState.BatteryCharge * 100);
        }

        private bool GetCharging(IState message)
        {
            if (message.BatteryState == null)
            {
                return false;
            }

            return message.BatteryState.Charging;
        }

        private bool GetDisturbed(IState message)
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