using Gebhardt.Acc.TestPlanRunner.Domain.Models;

namespace Gebhardt.Acc.TestPlanRunner.Messages.Responses;

/// <summary>
/// Response containing test plans.
/// </summary>
public record GotTestPlans(List<TestPlan> TestPlans);