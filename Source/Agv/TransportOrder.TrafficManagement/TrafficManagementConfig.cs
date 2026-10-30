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
    
    /// <summary>
    /// If true the last node id of the agv status is used to determine the current routing node. If false the X and Y coordinates are used.
    /// </summary>
    public static bool UseLastNodeIdForNodeFinding { get; set; } = false;

    public static double MaxDistanceToFindNode { get; set; } = 0.15;
    
    public static double MaxDistanceToKeepNode { get; set; } = 0.25;

    public static double MaxOrientationDeviationToFindNode { get; set; } = 0.15; //8.6°

    public static double MaxOrientationDeviationToKeepNode { get; set; } = 0.25; //14.3°
}