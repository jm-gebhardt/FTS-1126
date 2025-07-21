namespace Gebhardt.Acc.TransportOrder.WebApi.Constants;

/// <summary>
/// Provides constants for SignalR configuration in the Web API.
/// </summary>
public static class WebApiConstants
{
    /// <summary>
    /// <see cref="Microsoft.AspNetCore.SignalR.HubOptions.ClientTimeoutInterval"/>
    /// </summary>
    public const int ClientTimeoutSeconds = 30;

    /// <summary>
    /// <see cref="Microsoft.AspNetCore.SignalR.HubOptions.KeepAliveInterval"/>
    /// </summary>
    public const int KeepAliveIntervalSeconds = 15;

    /// <summary>
    /// <see cref="Microsoft.AspNetCore.SignalR.HubOptions.StatefulReconnectBufferSize"/>
    /// Typical downstream traffic: ~25 kB / 15 s for 5 AGVs (estimated)
    /// </summary>
    public const int StatefulReconnectBufferSize = 100_000;

    /// <summary>
    /// Max time a SignalR send can take before it is canceled.
    /// Should be above: <see cref="Microsoft.AspNetCore.SignalR.HubOptions.ClientTimeoutInterval"/> to allow for retries
    /// </summary>
    public const int HubInvocationTimeoutSeconds = 35;
}