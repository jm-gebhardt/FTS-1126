using System.Collections.ObjectModel;
using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Exceptions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;

/// <summary>
/// Provides a parser for the <see cref="MqttState"/> message.
/// </summary>
/// <inheritdoc cref="StateParser"/>>
public class StateParser(ILogger logger, IMapper mapper)
{
    /// <summary>
    /// The agv serial number.
    /// </summary>
    public string SerialNumber { get; private set; } = string.Empty;

    /// <summary>
    /// The correlationId of the agv.
    /// </summary>
    public Guid CorrelationId => SerialNumber.ToGuid();

    /// <summary>
    /// The agv cargos.
    /// </summary>
    public Collection<Cargo> Cargos { get; private set; } = [];

    /// <summary>
    /// The agv status.
    /// </summary>
    public AgvStatus AgvStatus { get; private set; } = null!;

    /// <summary>
    /// The agv position.
    /// </summary>
    public AgvPosition AgvPosition { get; private set; } = null!;

    /// <summary>
    /// The agv errors.
    /// </summary>
    public List<AgvError>? AgvErrors { get; private set; }

    /// <summary>
    /// Parse the <see cref="MqttState"/>.
    /// </summary>
    /// <param name="state">The state.</param>
    public void Parse(MqttState state)
    {
        GetSerialNumber(state);
        GetCargos(state);
        GetStatus(state);
        GetPosition(state);
        GetErrors(state);
        ConvertFieldViolationToWarning(state);
    }

    private void GetSerialNumber(MqttState state)
    {
        SerialNumber = state.SerialNumber;
        if (string.IsNullOrEmpty(SerialNumber))
        {
            logger.LogError("Missing serialNumber in {MessageType} message: {Status}", nameof(MqttState), state);
            throw new SerialNumberNotSetException(nameof(MqttState));
        }
    }

    private void GetCargos(MqttState state)
    {
        List<MqttLoad>? loads = state.Loads;
        if (loads == null)
        {
            return;
        }

        if (loads.Any(l => string.IsNullOrEmpty(l.LoadId)))
        {
            throw new ArgumentNullException(nameof(MqttLoad.LoadId), "LoadId must be set! This was the assumption at the time of implementation.");
        }

        Cargos = mapper.Map<Collection<Cargo>>(loads);
        foreach (var load in Cargos)
        {
            mapper.Map(CorrelationId, load);
        }
    }

    private void GetStatus(MqttState state)
    {
        AgvStatus = new AgvStatus(
            CorrelationId,
            state.OrderId,
            GetBusy(state),
            GetAutomatic(state),
            state.BatteryState?.BatteryCharge ?? -1f,
            state.BatteryState?.Charging ?? false,
            state.BatteryState?.BatteryVoltage ?? -1f,
            state.BatteryState?.BatteryCurrent ?? -1f,
            state.BatteryState?.BatteryTemperature ?? -1f,
            GetDisturbed(state),
            !string.IsNullOrEmpty(state.LastNodeId) ? state.LastNodeId : null
        );
    }

    private bool GetBusy(MqttState state)
    {
        if (state.ActionStates == null)
        {
            return false;
        }

        foreach (var actionState in state.ActionStates)
        {
            switch (actionState.ActionStatus)
            {
                case MqttActionStatus.Failed: break;
                case MqttActionStatus.Finished: break;
                case MqttActionStatus.Initializing: return true;
                case MqttActionStatus.Running: return true;
                case MqttActionStatus.Waiting: break;
            }
        }

        return false;
    }

    private bool GetAutomatic(MqttState state)
    {
        switch (state.OperatingMode)
        {
            case MqttOperatingMode.Automatic: return true;
            case MqttOperatingMode.Manual: return false;
            case MqttOperatingMode.Semiautomatic: return false;
            case MqttOperatingMode.Service: return false;
            case MqttOperatingMode.Teachin: return false;
            default: return false;
        }
    }

    private bool GetDisturbed(MqttState state)
    {
        if (state.SafetyState != null)
        {
            if (state.SafetyState.EStop != MqttEStop.None)
            {
                return true;
            }

            if (state.SafetyState.FieldViolation)
            {
                return true;
            }
        }

        return false;
    }

    private void GetPosition(MqttState state)
    {
        if (state.AgvPosition == null)
        {
            return;
        }
        AgvPosition = new AgvPosition(
            state.AgvPosition.PositionInitialized,
            state.AgvPosition.X,
            state.AgvPosition.Y * -1, // Special treatment for Karis AGV. Y Axis is inverted
            0f,
            (float)Mathematics.ConvertToRadians(90.0f) - state.AgvPosition.Theta, // Special treatment for Karis AGV. 90° in our System (AGV facing right) is 0° in our world
            state.AgvPosition.DeviationRange
        );
    }

    private void GetErrors(MqttState state)
    {
        List<MqttError>? errors = state.Errors;

        if (errors?.Any(l => string.IsNullOrEmpty(l.ErrorType)) ?? false)
        {
            throw new ArgumentNullException(nameof(MqttError.ErrorType), "ErrorType must be set! This was the assumption at the time of implementation.");
        }

        AgvErrors = errors?.Select(e =>
        {
            var agvError = mapper.Map<AgvError>(e);
            // overwrite AgvCorrelationId in a 2. step, as we are mapping from two sources to one destination, which isn't supported by AutoMapper
            mapper.Map(CorrelationId, agvError);
            return agvError;
        }).ToList();
    }

    private void ConvertFieldViolationToWarning(MqttState state)
    {
        if (!GetDisturbed(state))
        {
            return;
        }

        if (!state.SafetyState!.FieldViolation)
        {
            return;
        }

        var fieldViolationWarning = new FieldViolation(CorrelationId, state.Timestamp.DateTime);

        if (AgvErrors == null)
        {
            AgvErrors = [fieldViolationWarning];
            return;
        }
        AgvErrors.Add(fieldViolationWarning);
    }
}