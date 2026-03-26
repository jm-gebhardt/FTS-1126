using Gebhardt.Acc.TestPlanRunner.Domain.Models;

namespace Gebhardt.Acc.TestPlanRunner.Messages.Responses;

/// <summary>
/// Response containing a test plan.
/// </summary>
public record GotTestPlan(TestPlan? TestPlan);