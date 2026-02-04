using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Domain;

/// <summary>
/// Represents the execution state of a test plan for a specific AGV.
/// </summary>
[PublicAPI]
public class AgvExecutionState
{
    /// <summary>
    /// The AGV correlation Id.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The currently active test plan.
    /// </summary>
    public TestPlan? ActiveTestPlan { get; set; }

    /// <summary>
    /// A pending test plan that will replace the active one when safe.
    /// </summary>
    public TestPlan? PendingTestPlan { get; set; }

    /// <summary>
    /// The current iteration. (Total runs of the test plan)
    /// </summary>
    public int CurrentIterationIndex { get; set; }

    /// <summary>
    /// The current step index.
    /// </summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>
    /// The correlation Id of the currently executing action group.
    /// </summary>
    public Guid? CurrentActionGroupCorrelationId { get; set; }

    /// <summary>
    /// The type of the currently executing action.
    /// </summary>
    public ActionType? CurrentActionType { get; set; }

    /// <summary>
    /// Whether a cancellation of the current test plan is pending (waiting for safe cancellation point).
    /// </summary>
    public bool CancellationPending { get; set; }

    /// <summary>
    /// Whether the test plan execution is marked for removal (stopping execution of current test plan).
    /// </summary>
    public bool MarkedForRemoval { get; set; }

    /// <summary>
    /// Whether the AGV is currently idle and ready for the next action.
    /// </summary>
    public bool IsIdle { get; set; } = true;

    /// <summary>
    /// The wait time in seconds before starting the next step.
    /// </summary>
    public int PendingWaitTimeSeconds { get; set; }

    /// <summary>
    /// The time when the last action completed (used for wait time tracking).
    /// </summary>
    public DateTime? LastActionCompletedTime { get; set; }

    /// <summary>
    /// Gets the current step from the active test plan, or null if none.
    /// </summary>
    public TestPlanStep? GetCurrentStep()
    {
        if (ActiveTestPlan == null || CurrentStepIndex >= ActiveTestPlan.Steps.Count)
        {
            return null;
        }

        return ActiveTestPlan.Steps[CurrentStepIndex];
    }

    /// <summary>
    /// Advances to the next step, returning true if successful, false if test plan is completed.
    /// </summary>
    public bool AdvanceToNextStep()
    {
        if (ActiveTestPlan == null)
        {
            return false;
        }

        CurrentStepIndex++;

        if (CurrentStepIndex >= ActiveTestPlan.Steps.Count)
        {
            // We have completed all steps in the iteration, so start over
            CurrentStepIndex = 0;
            CurrentIterationIndex++;

            if (!ActiveTestPlan.HasInfiniteIterations && CurrentIterationIndex >= ActiveTestPlan.Iterations)
            {
                // Test plan complete
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Resets the execution state to start fresh with a new test plan.
    /// </summary>
    public void Reset(TestPlan? newTestPlan)
    {
        ActiveTestPlan = newTestPlan;
        PendingTestPlan = null;
        CurrentIterationIndex = 0;
        CurrentStepIndex = 0;
        CurrentActionGroupCorrelationId = null;
        CurrentActionType = null;
        CancellationPending = false;
        MarkedForRemoval = false;
        PendingWaitTimeSeconds = 0;
        LastActionCompletedTime = null;
    }

    /// <summary>
    /// Switches to the pending test plan.
    /// </summary>
    public void SwitchToPendingTestPlan()
    {
        if (PendingTestPlan != null)
        {
            Reset(PendingTestPlan);
        }
    }
}