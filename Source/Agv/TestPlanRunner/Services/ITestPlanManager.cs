using Gebhardt.Acc.TestPlanRunner.Domain;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Manages test plan execution for AGVs.
/// </summary>
public interface ITestPlanManager
{
    /// <summary>
    /// Applies a test plan, starting execution for idle AGVs or marking pending transition for busy ones.
    /// </summary>
    Task ApplyTestPlanAsync(TestPlan testPlan);

    /// <summary>
    /// Removes a test plan, stopping execution for affected AGVs.
    /// </summary>
    Task RemoveTestPlanAsync(string name);

    /// <summary>
    /// Gets all currently active test plans.
    /// </summary>
    IEnumerable<TestPlan> GetActiveTestPlans();

    /// <summary>
    /// Gets the active test plan by name.
    /// </summary>
    TestPlan? GetTestPlan(string name);

    /// <summary>
    /// Tries to start next test plan step of AGV for the specified test plan.
    /// </summary>
    Task TryStartAgvAsync(Guid agvCorrelationId);
}