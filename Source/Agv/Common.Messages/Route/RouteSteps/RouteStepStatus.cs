namespace Gebhardt.Acc.Common.Messages.Route.RouteSteps;

/// <summary>
/// Status of a route step.
/// </summary>
public enum RouteStepStatus
{
    /// <summary>
    /// The route step is planned by Traffic Management.
    /// </summary>
    Planned,

    /// <summary>
    /// The route step has been acknowledged by the AGV.
    /// </summary>
    Acknowledged,

    /// <summary>
    /// The route step has been started by the AGV.
    /// </summary>
    Started,

    /// <summary>
    /// The route step has been completed by the AGV successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The route step has failed.
    /// </summary>
    Failed
}