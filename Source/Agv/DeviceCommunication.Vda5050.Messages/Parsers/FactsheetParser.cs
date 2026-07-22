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
    public void Parse(MqttFactsheet factsheet)
    {
        GetSerialNumber(factsheet);
        var agv = GetAgv(factsheet);
        GetAgvData(factsheet, agv);
        GetAgvActions(factsheet);
    }

    private Agv GetAgv(MqttFactsheet factsheet)
    {
        if (Agv == null)
        {
            Agv = new Agv(CorrelationId, factsheet.Manufacturer, factsheet.TypeSpecification.SeriesName, SerialNumber);
            return Agv;
        }

        Agv.SeriesName = factsheet.TypeSpecification.SeriesName;
        return Agv;
    }

    private void GetSerialNumber(MqttFactsheet factsheet)
    {
        SerialNumber = factsheet.SerialNumber;
        if (string.IsNullOrEmpty(SerialNumber))
        {
            logger.LogError("Missing serialNumber in {MessageType} message: {Factsheet}", nameof(MqttFactsheet), factsheet);
            throw new SerialNumberNotSetException(nameof(MqttFactsheet));
        }
    }

    private void GetAgvData(MqttFactsheet factsheet, Agv agv)
    {
        AgvData = new AgvData(
            agvCorrelationId: CorrelationId,
            model: factsheet.TypeSpecification.SeriesName,
            type: agv.VehicleTypeId,
            ipV4Address: factsheet.VehicleConfig?.Network?.LocalIpAddress ?? string.Empty,
            softwareVersion: factsheet.VehicleConfig?.Versions != null ? factsheet.VehicleConfig!.Versions!.SingleOrDefault(v => string.Equals(v.Key, "softwareVersion", StringComparison.InvariantCultureIgnoreCase))?.Value ?? string.Empty : string.Empty,
            batteryCapacity: -1,
            acceleration: factsheet.PhysicalParameters.AccelerationMax,
            maxVelocityStraight: factsheet.PhysicalParameters.SpeedMax, // maybe get minimum of load speed limits
            maxVelocityTurn: 1000,
            maxVelocityTurnOnSpot: 1000,
            turnOnSpotThreshold: 1000,
            width: factsheet.PhysicalParameters.Width,
            length: factsheet.PhysicalParameters.Length,
            payload: (long)factsheet.TypeSpecification.MaxLoadMass,
            sensors: []
        );
    }

    private void GetAgvActions(MqttFactsheet factsheet)
    {
        if (factsheet.ProtocolFeatures.AgvActions.Count == 0)
        {
            return;
        }
        Actions = mapper.Map<ICollection<AgvAction>>(factsheet.ProtocolFeatures.AgvActions);
        foreach (var action in Actions)
        {
            action.AgvSerialNumber = SerialNumber;
            logger.LogInformation("Agv {AgvCorrelationId}/{AgvSerialNumber} supports action {ActionType} on scopes {ActionScopes}", CorrelationId, SerialNumber, action.ActionType, action.ActionScopes);
        }
    }
}