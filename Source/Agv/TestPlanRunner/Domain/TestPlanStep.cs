using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Domain;

/// <summary>
/// Represents a single step in a test plan.
/// </summary>
[PublicAPI]
public record TestPlanStep
{
    /// <summary>
    /// The type of step to perform.
    /// </summary>
    public required StepType StepType { get; init; }

    /// <summary>
    /// The node/station name where the action should be performed.
    /// </summary>
    public string? TargetNode { get; init; }

    /// <summary>
    /// The source where to pick up the order (for HostOrder step).
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// The destination where to drop the order (for HostOrder step).
    /// </summary>
    public string? Destination { get; init; }

    /// <summary>
    /// The rotation info for the orders handled box or transportation good in rad (for HostOrder step).
    /// </summary>
    public double? LeRotation { get; init; }

    /// <summary>
    /// Delay in seconds after completing this step before proceeding to next step.
    /// </summary>
    public int WaitTimeSeconds { get; init; }
}