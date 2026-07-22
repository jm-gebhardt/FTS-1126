using Gebhardt.Acc.Common.WebApi.Swagger;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs;

/// <summary>
/// Api Group for SignalR
/// </summary>
public class HubApiGroup : ApiGroup
{
    /// <summary>
    /// Api group name to be used with SignalRHub attribute
    /// </summary>
    public const string GroupName = "signalr";

    /// <inheritdoc />
    public override string Name => Version + GroupName;

    /// <inheritdoc />
    public override string Title => "SignalR Hub";

    /// <inheritdoc />
    public override string Version => "v1";
}