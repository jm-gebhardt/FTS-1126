using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TestPlanRunner.Domain;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Provides access to AGV execution states.
/// </summary>
public interface IAgvExecutionStateProvider : IConcurrentDictionaryProvider<AgvExecutionState>
{
    /// <summary>
    /// Gets or creates an execution state for the specified AGV.
    /// </summary>
    AgvExecutionState GetOrCreate(Guid agvCorrelationId);

    /// <summary>
    /// Gets all execution states.
    /// </summary>
    IEnumerable<AgvExecutionState> GetAll();

    /// <summary>
    /// Removes the execution state for the specified AGV.
    /// </summary>
    bool Remove(Guid agvCorrelationId);
}