using Gebhardt.Acc.TestPlanRunner.Domain;

namespace Gebhardt.Acc.TestPlanRunner.Services.Parsing;

/// <summary>
/// Factory for creating the appropriate parser.
/// </summary>
public interface ITestPlanParserFactory
{
    /// <summary>
    /// Gets all currently supported test plan file extensions.
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }

    /// <summary>
    /// Tries to parse a test plan file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the test plan file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed test plan if successful, null otherwise.</returns>
    Task<TestPlan?> TryParseAsync(string filePath, CancellationToken cancellationToken = default);
}