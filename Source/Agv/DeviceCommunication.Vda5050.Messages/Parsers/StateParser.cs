using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;

/// <summary>
/// Provides a parser for the <see cref="MqttState"/> message.
/// </summary>
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
    /// The current state of the order.
    /// </summary>
    public OrderState OrderState { get; private set; } = null!;

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
        GetInformation(state);
        ConvertFieldViolationToWarning(state);
        GetOrderState(state);
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
        var loads = state.Loads;
        if (loads == null)
        {
            return;
        }

        ValidateLoadIds(ref loads);

        Cargos = mapper.Map<Collection<Cargo>>(loads);
        foreach (var load in Cargos)
        {
            mapper.Map(CorrelationId, load);
        }
    }

    private void ValidateLoadIds(ref IList<MqttLoad>? loads)
    {
        if (loads == null)
        {
            return;
        }

        if (loads.Any(l => string.IsNullOrEmpty(l.LoadId)))
        {
            throw new ArgumentNullException(nameof(MqttLoad.LoadId), "LoadId must be set! This was the assumption at the time of implementation.");
        }
    }

    private void GetStatus(MqttState state)
    {
        AgvStatus = new AgvStatus(
            CorrelationId,
            state.OrderId,
            GetBusy(state),
            GetAutomatic(state),
            state.BatteryState.BatteryCharge,
            state.BatteryState.Charging,
            state.BatteryState.BatteryVoltage ?? 0f,
             -1f,
             -1f,
            GetDisturbed(state),
            !string.IsNullOrEmpty(state.LastNodeId) ? state.LastNodeId.ToGuid() : null,
            operatingMode: mapper.Map<AgvOperatingMode>(state.OperatingMode));
    }

    private bool GetBusy(MqttState state)
    {
        if (state.ActionStates.Count == 0)
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
        if (state.SafetyState.EStop != MqttEStop.None)
        {
            return true;
        }

        if (state.SafetyState.FieldViolation)
        {
            return true;
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
            (float)state.AgvPosition.X,
            (float)state.AgvPosition.Y ,
            0f,
            (float)state.AgvPosition.Theta, 
            (float?)state.AgvPosition.DeviationRange
        );
    }

    private void GetOrderState(MqttState state)
    {
        OrderState = new OrderState()
        {
            OrderId = state.OrderId,
            OrderUpdateId = state.OrderUpdateId,
            LastNodeSequenceId = state.LastNodeSequenceId,
            NewBaseRequest = state.NewBaseRequest,
            Errors = AgvErrors?.Where(e => Vda5050Specifications.ErrorTypes.IsOrderError(e.ErrorType)).ToList() ?? [],
            ActionStates = mapper.Map<List<OrderActionState>>(state.ActionStates)
        };
    }

    private void GetErrors(MqttState state)
    {
        var errors = state.Errors;

        if (errors.Any(l => string.IsNullOrEmpty(l.ErrorType)))
        {
            throw new ArgumentNullException(nameof(MqttError.ErrorType), "ErrorType must be set! This was the assumption at the time of implementation.");
        }

        AddErrors(errors);
    }

    private void GetInformation(MqttState state)
    {
        var infos = state.Information;
        if (infos == null)
        {
            return;
        }
        AddErrors(infos);
    }

    private void AddErrors<T>(ICollection<T> errors)
    {
        var errorInformation = errors.Select(i =>
        {
            var agvError = mapper.Map<AgvError>(i);
            // overwrite AgvCorrelationId in a 2. step, as we are mapping from two sources to one destination, which isn't supported by AutoMapper
            mapper.Map(CorrelationId, agvError);
            return agvError;
        }).ToList();

        if (AgvErrors == null)
        {
            AgvErrors = errorInformation;
            return;
        }
        AgvErrors.AddRange(errorInformation);
    }

    private void ConvertFieldViolationToWarning(MqttState state)
    {
        if (!GetDisturbed(state))
        {
            return;
        }

        if (!state.SafetyState.FieldViolation)
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