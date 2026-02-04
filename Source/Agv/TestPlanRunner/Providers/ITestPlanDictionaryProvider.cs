using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TestPlanRunner.Domain;

namespace Gebhardt.Acc.TestPlanRunner.Providers;

/// <summary>
/// Marker interface for the test plan dictionary provider.
/// Test plan by test plan name.
/// </summary>
public interface ITestPlanDictionaryProvider : IConcurrentDictionaryProvider<string, TestPlan>;