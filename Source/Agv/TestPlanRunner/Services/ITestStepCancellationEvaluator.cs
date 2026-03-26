using Gebhardt.Acc.TestPlanRunner.Domain.Models;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Evaluates whether a test step can be canceled.
/// For example a Drop action can not be canceled, as the Agv is loaded.
/// </summary>
public interface ITestStepCancellationEvaluator
{
    /// <summary>
    /// Evaluates if the current step of an AGV can be canceled.
    /// </summary>
    /// <param name="agvCorrelationId">The AGV correlation Id.</param>
    /// <param name="stepType">The current step type.</param>
    /// <returns>True if the step can be canceled, false otherwise.</returns>
    Task<bool> CanCancelAsync(Guid agvCorrelationId, StepType stepType);
}