using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;

/// <summary>
/// Hub for ActionGroup related Dtos
/// </summary>
[Authorize]
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