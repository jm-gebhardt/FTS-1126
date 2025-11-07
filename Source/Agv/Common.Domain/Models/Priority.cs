namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// The priority of an action group.
/// </summary>
public enum Priority
{
    /// <summary> Default priority. </summary>
    Default = 0,

    /// <summary> Resumed action group priority. </summary>
    Resume = 10,

    /// <summary> Queue evacuation priority. </summary>
    QueueEvacuation = 20,

    /// <summary> Queueing priority. </summary>
    Queueing = 30,

    /// <summary> Manual order priority. </summary>
    Manual = 40,

    /// <summary> Charging priority. </summary>
    Charging = 50
}