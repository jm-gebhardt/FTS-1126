using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

#pragma warning disable CS1591 // we do not have any documentation from FlexLog on the API
public record PinStatus
{
    [JsonPropertyName("id")]
    public string Id
    {
        get;
        [PublicAPI]
        init;
    }

    [JsonPropertyName("direction")]
    public string Direction
    {
        get;
        [PublicAPI]
        init;
    }

    [JsonPropertyName("result")]
    public string? Result
    {
        get;
        [PublicAPI]
        init;
    }

    [JsonPropertyName("state")]
    public bool? State
    {
        get;
        [PublicAPI]
        init;
    }

    [UsedImplicitly]
    public PinStatus()
    {
        Direction = null!;
        Id = null!;
    }

    [UsedImplicitly]
    public PinStatus(string id, string direction, string? result = null, bool? state = null)
    {
        Id = id;
        Direction = direction;
        Result = result;
        State = state;
    }
}
#pragma warning restore CS1591