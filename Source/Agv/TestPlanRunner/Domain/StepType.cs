using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Domain;

/// <summary>
/// The type of the test plan step.
/// </summary>
[PublicAPI]
public enum StepType
{
    /// <summary>
    /// Move AgvAction.
    /// </summary>
    Move,

    /// <summary>
    /// Pick AgvAction.
    /// </summary>
    Pick,

    /// <summary>
    /// Drop AgvAction.
    /// </summary>
    Drop,

    /// <summary>
    /// Charge AgvAction.
    /// </summary>
    Charge,

    /// <summary>
    /// Create a host order (pick + drop ActionGroup).
    /// </summary>
    HostOrder
}

