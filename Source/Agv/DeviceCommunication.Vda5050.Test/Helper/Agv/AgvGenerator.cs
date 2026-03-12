using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;

/// <summary>
/// Provides a helper for generating <see cref="Agv"/>s for unit testing.
/// </summary>
[PublicAPI]
public static class AgvGenerator
{
    /// <summary>
    /// Generate an agv.
    /// </summary>
    public static Vda5050.Domain.Models.Agvs.Agv Generate(Guid correlationId,
        string serialNumber = "agv1",
        Guid? currentOrder = null,
        Guid? currentFleetManagerActionId = null,
        string? interfaceName = null,
        string? protocolMajorVersion = null)
    {
        return new Vda5050.Domain.Models.Agvs.Agv(correlationId,
            "manufacturer",
            "series",
            serialNumber)
        {
            CurrentOrder = currentOrder,
            CurrentFleetManagerActionId = currentFleetManagerActionId,
            InterfaceName = interfaceName,
            ProtocolMajorVersion = protocolMajorVersion
        };
    }

    public static AgvAction GenerateNodeAction(string actionType = "pick", string description="description", string? resultDescription=null, string agvSerialNumber = "agv", BlockingType blockingType=BlockingType.NONE, ICollection<AgvActionParameter>? parameters = null)
    {
        return new AgvAction()
        {
            ActionType = actionType,
            ActionDescription = description,
            ActionScopes = [ActionScope.Node],
            BlockingTypes = [blockingType],
            ActionParameters = parameters,
            AgvSerialNumber = agvSerialNumber,
            ResultDescription = resultDescription
        };
    }
    
    public static AgvAction GenerateInstantAction(string actionType = "initPosition", string description = "description", string? resultDescription = null, string agvSerialNumber = "agv", BlockingType blockingType = BlockingType.NONE, ICollection<AgvActionParameter>? parameters = null)
    {
        return new AgvAction()
        {
            ActionType = actionType,
            ActionDescription = description,
            ActionScopes = [ActionScope.Instant],
            BlockingTypes = [blockingType],
            ActionParameters = parameters,
            AgvSerialNumber = agvSerialNumber,
            ResultDescription = resultDescription
        };
    }

    public static AgvAction InitPositionInstantAction = GenerateInstantAction(Vda5050Specifications.ActionTypes.InitPosition, "Resets (overrides) the pose of the AGV with the given parameters.", null, "serialNumber", BlockingType.HARD,
    [
        new AgvActionParameter()
        {
            IsOptional = false,
            Key = Vda5050Specifications.Parameters.X,
            ValueDataType = ValueDataType.Float
        },
        new AgvActionParameter()
        {
            IsOptional = false,
            Key = Vda5050Specifications.Parameters.Y,
            ValueDataType = ValueDataType.Float
        },
        new AgvActionParameter()
        {
            IsOptional = false,
            Key = Vda5050Specifications.Parameters.MapId,
            ValueDataType = ValueDataType.String
        },
        new AgvActionParameter()
        {
            IsOptional = false,
            Key = Vda5050Specifications.Parameters.Theta,
            ValueDataType = ValueDataType.Float
        },
        new AgvActionParameter()
        {
            IsOptional = false,
            Key = Vda5050Specifications.Parameters.LastNodeId,
            ValueDataType = ValueDataType.String
        }
    ]);
}