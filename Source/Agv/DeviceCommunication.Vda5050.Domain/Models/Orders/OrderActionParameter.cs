using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Key-value parameter for an order action.
/// </summary>
[PublicAPI]
public record OrderActionParameter
{
    /// <summary>
    /// The key of the action parameter.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value of the action parameter.
    /// Can be a string, number, boolean, array, or object.
    /// </summary>
    public object Value { get; set; } = new();
}