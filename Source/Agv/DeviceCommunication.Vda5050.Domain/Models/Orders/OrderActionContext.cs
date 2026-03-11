using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Provides a context for building order actions.
/// </summary>
[PublicAPI]
public record OrderActionContext
{
    /// <summary>
    /// Parameters for VDA5050 predefined actions.
    /// </summary>
    public Vda5050Parameters Vda5050Parameters { get; init; } = new();

    /// <summary>
    /// The blocking type of the action.
    /// </summary>
    public BlockingType? BlockingType { get; init; }

    /// <summary>
    /// Custom parameters of manufacturer defined order actions.
    /// </summary>
    public Dictionary<string, object>? CustomParameters { get; set; } = new();

    /// <inheritdoc cref="OrderActionContext"/>
    public OrderActionContext(ICollection<OrderActionParameter> parameters, BlockingType? blockingType, bool isVda5050Action)
    {
        BlockingType = blockingType;
        if (!isVda5050Action)
        {
            CustomParameters = parameters.ToDictionary(k => k.Key, v => v.Value);
            return;
        }
        foreach (var orderActionParameter in parameters)
        {
            SetVda5050Parameter(orderActionParameter);
        }
    }

    private void SetVda5050Parameter(OrderActionParameter parameter)
    {
        if (Vda5050Specifications.Parameters.StationType.Equals(parameter.Key))
        {
            Vda5050Parameters.StationType = (string?)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.StationName.Equals(parameter.Key))
        {
            Vda5050Parameters.StationName = (string?)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.LoadHandlingDevice.Equals(parameter.Key))
        {
            Vda5050Parameters.LoadHandlingDevice = (string?)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.MapId.Equals(parameter.Key))
        {
            Vda5050Parameters.MapId = (string?)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.MapVersion.Equals(parameter.Key))
        {
            Vda5050Parameters.MapVersion = (string?)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.LastNodeId.Equals(parameter.Key))
        {
            Vda5050Parameters.LastNodeId = (string?)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.ObjectType.Equals(parameter.Key))
        {
            Vda5050Parameters.ObjectType = (string?)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.TriggerType.Equals(parameter.Key))
        {
            Vda5050Parameters.TriggerType = (string?)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.X.Equals(parameter.Key))
        {
            Vda5050Parameters.Position ??= new Vda5050Position();
            Vda5050Parameters.Position.X = (float) parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.Y.Equals(parameter.Key))
        {
            Vda5050Parameters.Position ??= new Vda5050Position();
            Vda5050Parameters.Position.Y = (float)parameter.Value;
            return;
        }
        if (Vda5050Specifications.Parameters.Theta.Equals(parameter.Key))
        {
            Vda5050Parameters.Position ??= new Vda5050Position();
            Vda5050Parameters.Position.Theta = (float)parameter.Value;
            return;
        }
        CustomParameters ??= new Dictionary<string, object>(); 
        CustomParameters?.TryAdd(parameter.Key, parameter.Value);
    }
}