using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using JetBrains.Annotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gebhardt.Acc.TestPlanRunner.Domain.Parsing;

/// <summary>
/// Parser for YAML test plan files.
/// </summary>
[UsedImplicitly]
public class YamlTestPlanParser : ITestPlanParser
{
    private readonly ILogger<YamlTestPlanParser> _logger;
    private readonly IDeserializer _deserializer;

    /// <inheritdoc cref="YamlTestPlanParser"/>
    public YamlTestPlanParser(ILogger<YamlTestPlanParser> logger)
    {
        _logger = logger;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
    }

    /// <inheritdoc />
    public string[] SupportedExtensions => [".yaml", ".yml"];

    /// <inheritdoc />
    public Task<TestPlan?> ParseAsync(string content)
    {
        try
        {
            var result = _deserializer.Deserialize<TestPlan>(content);
            return Task.FromResult<TestPlan?>(result);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Error parsing YAML test plan");
            return Task.FromResult<TestPlan?>(null);
        }
    }
}