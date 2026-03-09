using Gebhardt.Acc.TestPlanRunner.Domain;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Services.Parsing;

/// <summary>
/// Factory for creating test plan parsers based on file extension.
/// </summary>
[UsedImplicitly]
public class TestPlanParserFactory : ITestPlanParserFactory
{
    private readonly ILogger<TestPlanParserFactory> _logger;
    private readonly Dictionary<string, ITestPlanParser> _parsers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => _parsers.Keys;

    /// <inheritdoc cref="TestPlanParserFactory"/>
    public TestPlanParserFactory(
        ILogger<TestPlanParserFactory> logger,
        JsonTestPlanParser jsonParser,
        YamlTestPlanParser yamlParser)
    {
        _logger = logger;

        RegisterParser(jsonParser);
        RegisterParser(yamlParser);
    }

    private void RegisterParser(ITestPlanParser parser)
    {
        foreach (string extension in parser.SupportedExtensions)
        {
            _parsers[extension] = parser;
        }
    }

    private ITestPlanParser? GetParser(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return _parsers.GetValueOrDefault(extension);
    }

    /// <inheritdoc />
    public async Task<TestPlan?> TryParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var parser = GetParser(filePath);
        if (parser == null)
        {
            _logger.LogError("No parser found for file extension: {FilePath}", filePath);
            return null;
        }

        try
        {
            string content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var testPlan = await parser.ParseAsync(content);

            if (testPlan == null)
            {
                _logger.LogError("Failed to parse test plan file: {FilePath}", filePath);
                return null;
            }

            var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
            string testPlanName = Path.GetFileNameWithoutExtension(filePath);
            testPlan = testPlan with { Name = testPlanName, LastWriteTime = lastWriteTime };

            if (!ValidateTestPlan(testPlan, filePath))
            {
                return null;
            }

            _logger.LogDebug("Successfully parsed test plan: {TestPlanName} from {FilePath}", testPlan.Name, filePath);
            return testPlan;
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "File is locked or being written, skipping: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing test plan file: {FilePath}", filePath);
            return null;
        }
    }

    private bool ValidateTestPlan(TestPlan testPlan, string filePath)
    {
        if (string.IsNullOrWhiteSpace(testPlan.Name))
        {
            _logger.LogError("Test plan must have a name: {FilePath}", filePath);
            return false;
        }

        if (testPlan.Steps.Count == 0)
        {
            _logger.LogError("Test plan must have at least one step: {FilePath}", filePath);
            return false;
        }

        for (int i = 0; i < testPlan.Steps.Count; i++)
        {
            var step = testPlan.Steps[i];

            if (step.StepType == StepType.HostOrder)
            {
                if (string.IsNullOrWhiteSpace(step.Source))
                {
                    _logger.LogError("HostOrder step {StepIndex} must have a source: {FilePath}", i, filePath);
                    return false;
                }
                if (string.IsNullOrWhiteSpace(step.Destination))
                {
                    _logger.LogError("HostOrder step {StepIndex} must have a destination: {FilePath}", i, filePath);
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(step.TargetNode))
                {
                    _logger.LogError("HostOrder step {StepIndex} must not have a target node: {FilePath}", i, filePath);
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(step.TargetNode))
                {
                    _logger.LogError("Step {StepIndex} must have a target node: {FilePath}", i, filePath);
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(step.Source))
                {
                    _logger.LogError("HostOrder step {StepIndex} must not have a source: {FilePath}", i, filePath);
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(step.Destination))
                {
                    _logger.LogError("HostOrder step {StepIndex} must not have a destination: {FilePath}", i, filePath);
                    return false;
                }
            }

            if (step.WaitTimeSeconds < 0)
            {
                _logger.LogError("Step {StepIndex} wait time must be >= 0: {FilePath}", i, filePath);
                return false;
            }
        }

        if (testPlan.Iterations < 0)
        {
            _logger.LogError("Test plan iterations must be >= 0: {FilePath}", filePath);
            return false;
        }

        return true;
    }
}