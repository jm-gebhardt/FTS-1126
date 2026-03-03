namespace Gebhardt.Acc.TestPlanRunner.Messages.Requests;

/// <summary>
/// Request to get a specific test plan by name.
/// </summary>
public record GetTestPlan(string TestPlanName);