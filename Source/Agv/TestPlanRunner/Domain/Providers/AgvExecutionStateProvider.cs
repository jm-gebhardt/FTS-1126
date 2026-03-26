using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Providers;

/// <summary>
/// Implementation of <see cref="IAgvExecutionStateProvider"/>.
/// </summary>
public class AgvExecutionStateProvider : ConcurrentDictionaryProvider<AgvExecutionState>, IAgvExecutionStateProvider
{
    /// <inheritdoc />
    public AgvExecutionState GetOrCreate(Guid agvCorrelationId)
    {
        return ConcurrentDict.GetOrAdd(agvCorrelationId, id => new AgvExecutionState
        {
            AgvCorrelationId = id
        });
    }

    /// <inheritdoc />
    public IEnumerable<AgvExecutionState> GetAll()
    {
        return ConcurrentDict.Values;
    }

    /// <inheritdoc />
    public bool Remove(Guid agvCorrelationId)
    {
        return ConcurrentDict.TryRemove(agvCorrelationId, out _);
    }
}
