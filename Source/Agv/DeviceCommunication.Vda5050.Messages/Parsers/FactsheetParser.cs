using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;

/// <summary>
/// Provides a parser for the <see cref="MqttFactsheet"/> message.
/// </summary>
public class FactsheetParser(ILogger logger, IMapper mapper)
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
    /// The agv data.
    /// </summary>
    public AgvData AgvData { get; private set; } = null!;

    /// <summary>
    /// The agv.
    /// </summary>
    public Agv? Agv { get; set; }

    /// <summary>
    /// The agv actions.
    /// </summary>
    public ICollection<AgvAction> Actions { get; private set; } = [];

    /// <summary>
    /// Parse the <see cref="MqttFactsheet"/> .
    /// </summary>
    public void Parse(MqttFactsheet factSheet)
    {
        GetSerialNumber(factSheet);
        GetAgv(factSheet);
        GetAgvData(factSheet);
        GetAgvActions(factSheet);
    }

    private void GetAgv(MqttFactsheet factSheet)
    {
        if (Agv == null)
        {
            Agv = new Agv(CorrelationId, factSheet.Manufacturer, factSheet.TypeSpecification.SeriesName, SerialNumber);
            return;
        }
        Agv.SeriesName = factSheet.TypeSpecification.SeriesName;
    }

    private void GetSerialNumber(MqttFactsheet factSheet)
    {
        SerialNumber = factSheet.SerialNumber;
        if (string.IsNullOrEmpty(SerialNumber))
        {
            logger.LogError("Missing serialNumber in {MessageType} message: {FactSheet}", nameof(MqttFactsheet), factSheet);
            throw new SerialNumberNotSetException(nameof(MqttFactsheet));
        }
    }

    private void GetAgvData(MqttFactsheet factSheet)
    {
        AgvData = new AgvData(
            agvCorrelationId: CorrelationId,
            model: factSheet.TypeSpecification.SeriesName,
            type: factSheet.TypeSpecification.AgvClass.ToString(),
            ipV4Address: factSheet.VehicleConfig?.Network?.LocalIpAddress ?? string.Empty,
            softwareVersion: factSheet.VehicleConfig?.Versions != null ? factSheet.VehicleConfig!.Versions!.SingleOrDefault(v => string.Equals(v.Key, "softwareVersion", StringComparison.InvariantCultureIgnoreCase))?.Value ?? string.Empty : string.Empty,
            batteryCapacity: -1,
            acceleration: factSheet.PhysicalParameters.AccelerationMax,
            maxVelocityStraight: factSheet.PhysicalParameters.SpeedMax, // maybe get minimum of load speed limits
            maxVelocityTurn: 1000,
            maxVelocityTurnOnSpot: 1000,
            turnOnSpotThreshold: 1000,
            width: factSheet.PhysicalParameters.Width,
            length: factSheet.PhysicalParameters.Length,
            payload: (long)factSheet.TypeSpecification.MaxLoadMass,
            sensors: []
        );
    }

    private void GetAgvActions(MqttFactsheet factSheet)
    {
        if (factSheet.ProtocolFeatures.AgvActions.Count == 0)
        {
            return;
        }
        Actions = mapper.Map<ICollection<AgvAction>>(factSheet.ProtocolFeatures.AgvActions);
        foreach (var action in Actions)
        {
            action.AgvSerialNumber = SerialNumber;
            logger.LogInformation("Agv {AgvCorrelationId}/{AgvSerialNumber} supports action {ActionType} on scopes {ActionScopes}", CorrelationId, SerialNumber, action.ActionType, action.ActionScopes);
        }
    }
}