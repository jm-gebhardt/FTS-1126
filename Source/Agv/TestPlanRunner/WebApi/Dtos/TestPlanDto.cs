using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.WebApi.Dtos;

/// <summary>
/// Dto for a test plan.
/// </summary>
[PublicAPI]
public record TestPlanDto
{
    /// <summary>
    /// Unique identifier of the test plan.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// AGV serial numbers this test plan applies to. Empty/null means all AGVs.
    /// </summary>
    public Collection<string>? AgvSerialNumbers { get; init; }

    /// <summary>
    /// Number of times to execute the entire test plan. Null or 0 means infinite.
    /// </summary>
    public int? Iterations { get; init; }

    /// <summary>
    /// Steps to execute.
    /// </summary>
    public required List<TestPlanStepDto> Steps { get; init; }
}