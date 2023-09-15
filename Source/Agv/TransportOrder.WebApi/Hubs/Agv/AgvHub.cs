using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Hub for Agv related Dtos
/// </summary>
public class AgvHub : BaseHub<IAgvClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/agv";

    /// <inheritdoc cref="AgvHub"/>
    public AgvHub(ILogger<AgvHub> logger) : base(logger)
    {
    }
}