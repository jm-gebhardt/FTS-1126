using FluentValidation;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Parsing;

/// <summary>
/// Factory for creating test plan parsers based on file extension.
/// </summary>
[UsedImplicitly]
public class TestPlanParserFactory : ITestPlanParserFactory
{
    private readonly ILogger<TestPlanParserFactory> _logger;
    private readonly Dictionary<string, ITestPlanParser> _parsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IValidator<TestPlan> _validator;

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => _parsers.Keys;

    /// <inheritdoc cref="TestPlanParserFactory"/>
    public TestPlanParserFactory(
        ILogger<TestPlanParserFactory> logger,
        JsonTestPlanParser jsonParser,
        YamlTestPlanParser yamlParser,
        IValidator<TestPlan> validator)
    {
        _logger = logger;
        _validator = validator;

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

            var validationResult = await _validator.ValidateAsync(testPlan, cancellationToken);
            if (!validationResult.IsValid)
            {
                validationResult.Errors.ForEach(e => _logger.LogError(e.ErrorMessage));
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
}