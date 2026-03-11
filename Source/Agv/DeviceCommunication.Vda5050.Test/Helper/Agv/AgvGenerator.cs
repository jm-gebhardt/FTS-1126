using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;

/// <summary>
/// Provides a helper for generating <see cref="Agv"/>s for unit testing.
/// </summary>
public static class AgvGenerator
{
    /// <summary>
    /// Generate an agv.
    /// </summary>
    public static Vda5050.Domain.Models.Agvs.Agv Generate(Guid correlationId, string serialNumber = "agv1", Guid? currentOrder = null, Guid? currentFleetManagerActionId = null)
    {
        return new Vda5050.Domain.Models.Agvs.Agv(correlationId, "manufacturer", "series", serialNumber) { CurrentOrder = currentOrder, CurrentFleetManagerActionId = currentFleetManagerActionId };
    }

    public static AgvAction GenerateNodeAction(string actionType = "pick", string agvSerialNumber = "agv", BlockingType blockingType=BlockingType.NONE, ICollection<AgvActionParameter>? parameters = null)
    {
        return new AgvAction()
        {
            ActionType = actionType,
            ActionDescription = "description",
            ActionScopes = [ActionScope.Node],
            BlockingTypes = [blockingType],
            ActionParameters = parameters,
            AgvSerialNumber = agvSerialNumber,
            ResultDescription = "result"
        };
    }
}