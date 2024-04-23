using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.SmartLinkCommunication.Models;

#pragma warning disable CS1591 // we do not have any documentation from FlexLog on the API
[method: UsedImplicitly]
public record PinStatus(string Id, string Direction, string? Result = null, bool? State = null)
{
    [JsonPropertyName("id")]
    public string Id
    {
        get;
        [PublicAPI]
        init;
    } = Id;

    [JsonPropertyName("direction")]
    public string Direction
    {
        get;
        [PublicAPI]
        init;
    } = Direction;

    [JsonPropertyName("result")]
    public string? Result
    {
        get;
        [PublicAPI]
        init;
    } = Result;

    [JsonPropertyName("state")]
    public bool? State
    {
        get;
        [PublicAPI]
        init;
    } = State;

    [UsedImplicitly]
    public PinStatus() : this(null!, null!)
    {
    }
}
#pragma warning restore CS1591