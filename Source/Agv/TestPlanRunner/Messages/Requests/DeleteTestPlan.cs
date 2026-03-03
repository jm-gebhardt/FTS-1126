using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Messages.Requests;

/// <summary>
/// Request to delete a test plan by name.
/// </summary>
[PublicAPI]
public record DeleteTestPlan(string TestPlanName);