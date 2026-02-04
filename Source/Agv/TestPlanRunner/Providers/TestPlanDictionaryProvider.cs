using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TestPlanRunner.Domain;

namespace Gebhardt.Acc.TestPlanRunner.Providers;

/// <summary>
/// Implementation of <see cref="ITestPlanDictionaryProvider"/>.
/// </summary>
public class TestPlanDictionaryProvider : ConcurrentDictionaryProvider<string, TestPlan>, ITestPlanDictionaryProvider;