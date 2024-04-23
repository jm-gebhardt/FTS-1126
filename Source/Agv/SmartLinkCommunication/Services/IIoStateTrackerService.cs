using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

/// <summary>
/// Provides access to the IoStateTrackers of the module in the current scope
/// </summary>
public interface IIoStateTrackerService
{
    /// <summary>
    /// IoStateTrackers of the module in the current scope
    /// </summary>
    Dictionary<IoActionConfig, IoStateTracker> IoStateTrackers { get; }
}