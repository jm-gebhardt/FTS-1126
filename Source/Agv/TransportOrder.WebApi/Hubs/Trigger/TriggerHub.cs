using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;

/// <summary>
/// Hub for trigger related Dtos
/// </summary>
[Authorize]
public class TriggerHub: BaseHub<ITriggerClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/trigger";

    /// <inheritdoc cref="TriggerHub"/>
    public TriggerHub(ILogger<TriggerHub> logger) : base(logger)
    {
    }
}