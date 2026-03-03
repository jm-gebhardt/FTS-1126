using Gebhardt.Acc.TestPlanRunner.Domain;

namespace Gebhardt.Acc.TestPlanRunner.Messages.Responses;

/// <summary>
/// Response containing a test plan.
/// </summary>
public record GotTestPlan(TestPlan? TestPlan);