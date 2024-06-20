using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;

/// <summary>
/// Hub for map/graph/node related notifications.
/// </summary>
[Authorize]
public class MapHub : BaseHub<IMapClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/map";

    /// <inheritdoc cref="MapHub"/>
    public MapHub(ILogger<MapHub> logger) : base(logger)
    {
    }
}