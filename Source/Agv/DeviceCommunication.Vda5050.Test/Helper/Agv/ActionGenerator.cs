using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;

/// <summary>
/// Provides a helper for generating <see cref="AgvAction"/>s for unit testing.
/// </summary>
[PublicAPI]
public static class ActionGenerator
{
    public static AgvAction CreateAction(string serialNumber, string actionType, string description, ICollection<ActionScope> scopes, ICollection<BlockingType> blockingTypes, string parameterKey)
    {
        return new AgvAction
        {
            AgvSerialNumber = serialNumber,
            ActionType = actionType,
            ActionDescription = description,
            ActionScopes = scopes,
            BlockingTypes = blockingTypes,
            ActionParameters =
            [
                new AgvActionParameter
                {
                    Key = parameterKey,
                    ValueDataType = ValueDataType.String,
                    IsOptional = false
                }
            ]
        };
    }
}