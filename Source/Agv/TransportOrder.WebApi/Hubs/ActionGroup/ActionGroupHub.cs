using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;

/// <summary>
/// Hub for ActionGroup related Dtos
/// </summary>
public class ActionGroupHub : BaseHub<IActionGroupClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/action";

    /// <inheritdoc cref="ActionGroupHub"/>
    public ActionGroupHub(ILogger<ActionGroupHub> logger) : base(logger)
    {
    }
}