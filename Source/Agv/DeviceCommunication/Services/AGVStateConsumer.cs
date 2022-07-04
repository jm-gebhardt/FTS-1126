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
    public class AgvStateConsumer : IConsumer<IState>
    {
        private readonly ILogger<AgvStateConsumer> _logger;

        /// <summary>
        /// Create consumer for agv status message (<see cref="IState"/>)
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
            string? serialNumber = GetSerialNumber(message);

            if (string.IsNullOrEmpty(serialNumber))
            {
                _logger.LogError("Missing serialNumber in IState message: {Status}", context.Message);
                throw new InvalidOperationException($"Cannot handle {typeof(IState)} message, serialNumber must not be null or empty"); 
            }
           
            await context.Publish<IAgvStatusReceived>(new
            {
                CorrelationId = serialNumber.ToGuid(),
                AgvId = serialNumber,
                OrderId = GetOrderId(message),
                Busy = GetBusy(message),
                Automatic = GetAutomatic(message),
                BatteryCharge = GetBatteryCharge(message),
                Charging = GetCharging(message),
                Disturbed = GetDisturbed(message)
            }, context.CancellationToken);
            
        }

        private string? GetSerialNumber(IState message)
        {
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