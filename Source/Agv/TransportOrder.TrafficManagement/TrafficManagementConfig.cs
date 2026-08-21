namespace Gebhardt.Acc.TransportOrder.TrafficManagement;

/// <summary>
/// Central place for the configuration of traffic management.
/// </summary>
public static class TrafficManagementConfig
{
    /// <summary>
    /// The maximum combined length of RouteSteps an AGV is allowed to reserve at a time.
    /// </summary>
    public static double MaxReservationLength { get; set; } = 8;
}