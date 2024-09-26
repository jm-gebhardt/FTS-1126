using Microsoft.Extensions.Configuration;

namespace Gebhardt.Acc.TransportOrder.Core.Configurations;

/// <summary>
/// Provides order selection modes.
/// </summary>
public static class OrderSelectionMode
{
    /// <summary>Take the order with the fastest route based on a* </summary>
    public const string FastestRoute = "FASTESTROUTE";

    /// <summary> Take the oldest order </summary>
    public const string OldestOrder = "OLDESTORDER";

    /// <summary> Alternate between: fastest route and oldest order mode </summary>
    public const string Alternating = "ALTERNATING";

    /// <summary> Take the fastest of the x (=2) oldest orders </summary>
    public const string FastestOfXOldest = "FASTESTOFXOLDEST";

    /// <summary> Take the fastest order, if there are orders older than 2min, take the oldest of those</summary>
    public const string Threshold = "THRESHOLD";

    /// <summary> Take order with smallest score : Score = cost[s] / (1 + waiting time[min]**2)</summary>
    public const string Dynamic = "Dynamic";

    /// <summary>
    /// Get the order selection mode from the service configuration.
    /// </summary>
    public static string GetModeFromConfiguration(IConfiguration configuration)
    {
        return configuration.GetSection("ORDERSELECTION").GetValue("MODE", FastestRoute)?.ToUpper() ?? FastestRoute;
    }
}