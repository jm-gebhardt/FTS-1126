using Gebhardt.Acc.TestPlanRunner.Domain;

namespace Gebhardt.Acc.TestPlanRunner.Messages.Responses;

/// <summary>
/// Response containing test plans.
/// </summary>
public record GotTestPlans(List<TestPlan> TestPlans);