using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace Gebhardt.Acc.TestPlanRunner.Domain;

/// <summary>
/// Represents a test plan for AGV execution.
/// </summary>
[PublicAPI]
public record TestPlan
{
    /// <summary>
    /// Unique identifier for the test plan.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// AGV Id's this test plan applies to. Empty/null means all AGVs.
    /// </summary>
    [JsonIgnore]
    [YamlIgnore]
    public Collection<Guid>? AgvCorrelationIds { get; init; }

    /// <summary>
    /// AGV serial numbers this test plan applies to. Automatically converted to AgvCorrelationIds.
    /// </summary>
    public Collection<string>? AgvSerialNumbers
    {
        get;
        init
        {
            field = value;
            if (value != null)
            {
                AgvCorrelationIds = new Collection<Guid>(value.Select(x => x.ToGuid()).ToList());
            }
        }
    }

    /// <summary>
    /// Number of times to execute the entire test plan. Null or 0 means infinite.
    /// </summary>
    public int? Iterations { get; init; }

    /// <summary>
    /// Steps to execute.
    /// </summary>
    public required Collection<TestPlanStep> Steps { get; init; }

    /// <summary>
    /// The last update time of the test plan was updated.
    /// </summary>
    public DateTime LastUpdateTime { get; init; }

    /// <summary>
    /// Returns true if the test plan applies to all AGVs.
    /// </summary>
    public bool AppliesToAllAgvs => AgvCorrelationIds == null || AgvCorrelationIds.Count == 0;

    /// <summary>
    /// Returns true if the test plan has infinite iterations.
    /// </summary>
    public bool HasInfiniteIterations => Iterations is null or 0;

    /// <summary>
    /// Checks if this test plan applies to a specific AGV.
    /// </summary>
    public bool AppliesToAgv(Guid agvCorrelationId)
    {
        return AppliesToAllAgvs || AgvCorrelationIds!.Contains(agvCorrelationId);
    }
}