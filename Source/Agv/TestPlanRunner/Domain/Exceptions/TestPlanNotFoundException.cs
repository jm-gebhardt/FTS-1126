using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Exceptions;

/// <summary>
/// Exception thrown when a test plan is not found.
/// </summary>
public class TestPlanNotFoundException : NotFoundException
{
    /// <inheritdoc cref="TestPlanNotFoundException"/>
    /// <param name="testPlanName">The name of the test plan.</param>
    public TestPlanNotFoundException(string testPlanName)
        : base("TestPlan", nameof(testPlanName), testPlanName)
    {
    }
}