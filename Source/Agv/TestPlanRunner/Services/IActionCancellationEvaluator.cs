using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Evaluates whether an action can be canceled.
/// For example a Drop action can not be canceled, as the Agv is loaded.
/// </summary>
public interface IActionCancellationEvaluator
{
    /// <summary>
    /// Evaluates if the current action of an AGV can be canceled.
    /// </summary>
    /// <param name="agvCorrelationId">The AGV correlation Id.</param>
    /// <param name="actionType">The current action type.</param>
    /// <returns>True if the action can be canceled, false otherwise.</returns>
    Task<bool> CanCancelAsync(Guid agvCorrelationId, ActionType actionType);
}