using Gebhardt.Acc.TestPlanRunner.Domain;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Messages.Requests;

/// <summary>
/// Request to set (create or update) a test plan.
/// </summary>
/// <param name="TestPlan">The test plan.</param>
[PublicAPI]
public record SetTestPlan(TestPlan TestPlan);