namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Defines a result for the GetMapList request
/// </summary>
public record GotMapList
{
    /// <summary>
    /// List of maps.
    /// </summary>
    public List<Map> Maps { get; init; } = null!;
}