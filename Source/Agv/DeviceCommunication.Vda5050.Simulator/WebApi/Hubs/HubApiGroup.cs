using Gebhardt.Acc.Common.WebApi.Swagger;
using JetBrains.Annotations;

namespace DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;

/// <summary>
/// Api Group for SignalR
/// </summary>
[PublicAPI]
public class HubApiGroup : ApiGroup
{
    /// <summary>
    /// Api group name to be used with SignalRHub attribute
    /// </summary>
    public const string GroupName = "signalr";

    /// <inheritdoc />
    public override string Name => GroupName;

    /// <inheritdoc />
    public override string Title => "SignalR Hub";

    /// <inheritdoc />
    public override string Version => "v1";
}
