using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Provides extension methods for the action type.
/// </summary>
public static class ActionTypeExtension
{
    extension(ActionType actionType)
    {
        /// <summary>
        /// True, if the type of the current action matches a pre-define vda 5050 type.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public bool MatchesVda5050Type(string typeName)
        {
            if (Vda5050Specifications.ActionTypes.Pick.Equals(typeName) && actionType == ActionType.Pick)
            {
                return true;
            }
            if (Vda5050Specifications.ActionTypes.Drop.Equals(typeName) && actionType == ActionType.Drop)
            {
                return true;
            }

            if (Vda5050Specifications.ActionTypes.StartCharging.Equals(typeName) && actionType == ActionType.Charge)
            {
                return true;
            }
            return false;
        }
    }
}