using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.Vda5050Simulator.WebApi.Hubs;

/// <summary>
/// Hub for simulated agv state.
/// </summary>
[Authorize]
public class SimulatedAgvStateHub : Hub<ISimulatedAgvStateClient>
{
    /// <summary>
    /// Url path.
    /// </summary>
    public const string UrlPath = "/hub/state";
}
