using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Error severity levels.
/// </summary>
[PublicAPI]
public enum ErrorLevel
{
    /// <summary>
    /// AGV is not in running condition.
    /// </summary>
    Fatal,

    /// <summary>
    /// AGV is ready to drive without human intervention.
    /// </summary>
    Warning,

    /// <summary>
    /// AGV is ready and shares information relevant to the operator.
    /// </summary>
    Information
}