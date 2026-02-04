using Gebhardt.Acc.TestPlanRunner.Domain;

namespace Gebhardt.Acc.TestPlanRunner.Services.Parsing;

/// <summary>
/// Interface for parsing test plan files.
/// </summary>
public interface ITestPlanParser
{
    /// <summary>
    /// Gets the file extensions this parser supports (e.g., ".json", ".yaml").
    /// </summary>
    string[] SupportedExtensions { get; }

    /// <summary>
    /// Parses a test plan from file content.
    /// </summary>
    /// <param name="content">The file content to parse.</param>
    /// <returns>The parsed test plan, or null if parsing failed.</returns>
    Task<TestPlan?> ParseAsync(string content);
}