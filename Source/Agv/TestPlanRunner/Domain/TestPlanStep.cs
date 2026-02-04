using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Domain;

/// <summary>
/// Represents a single step in a test plan.
/// </summary>
[PublicAPI]
public record TestPlanStep
{
    /// <summary>
    /// The node/station name where the action should be performed.
    /// </summary>
    public required string TargetNode { get; init; }

    /// <summary>
    /// The action type to perform.
    /// </summary>
    public required ActionType ActionType { get; init; }

    /// <summary>
    /// Delay in seconds after completing this step before proceeding to next step.
    /// </summary>
    public int WaitTimeSeconds { get; init; }
}