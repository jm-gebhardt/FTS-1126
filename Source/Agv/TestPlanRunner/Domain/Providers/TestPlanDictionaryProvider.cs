using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Providers;

/// <summary>
/// Implementation of <see cref="ITestPlanDictionaryProvider"/>.
/// </summary>
public class TestPlanDictionaryProvider : ConcurrentDictionaryProvider<string, TestPlan>, ITestPlanDictionaryProvider;