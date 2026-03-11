using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Provides extension methods for order action parameters.
/// </summary>
public static class OrderActionParameterExtension
{
    extension(ICollection<OrderActionParameter> parameters)
    {
        /// <summary>
        /// Add the parameter given by key, and value to the parameter list, if it is required for the given agv action.
        /// </summary>
        /// <param name="agvAction">The agv action.</param>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        public void AddIfRequired(AgvAction agvAction, string key, object value)
        {
            if (agvAction.IsParameterRequired(key))
            {
                parameters.Add(new OrderActionParameter { Key = key, Value = value });
            }
        }
    }
}