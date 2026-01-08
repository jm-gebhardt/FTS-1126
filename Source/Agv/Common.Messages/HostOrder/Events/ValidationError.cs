namespace Gebhardt.Acc.Common.Messages.HostOrder.Events;

/// <summary>
/// Host order validation errors.
/// </summary>
public enum ValidationError
{
    /// <summary>
    /// The source of the order is unknown in this configuration.
    /// </summary>
    SourceUnknown,

    /// <summary>
    /// The target of the order is unknown in this configuration.
    /// </summary>
    TargetUnknown,

    /// <summary>
    /// There is no agv that can run from source to target.
    /// </summary>
    NoRoute
}