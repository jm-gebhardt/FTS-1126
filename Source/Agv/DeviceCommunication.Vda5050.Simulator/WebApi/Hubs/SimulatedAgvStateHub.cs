using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs;

/// <summary>
/// Hub for simulated agv VDA5050 state.
/// </summary>
[Authorize]
public class SimulatedAgvStateHub : Hub<ISimulatedAgvStateClient>
{
    /// <summary>
    /// Url path.
    /// </summary>
    public const string UrlPath = "/hub/state";
}