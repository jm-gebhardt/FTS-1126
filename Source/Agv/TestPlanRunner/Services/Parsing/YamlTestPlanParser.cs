using Gebhardt.Acc.TestPlanRunner.Domain;
using JetBrains.Annotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gebhardt.Acc.TestPlanRunner.Services.Parsing;

/// <summary>
/// Parser for YAML test plan files.
/// </summary>
[UsedImplicitly]
public class YamlTestPlanParser : ITestPlanParser
{
    private readonly IDeserializer _deserializer;

    /// <inheritdoc cref="YamlTestPlanParser"/>
    public YamlTestPlanParser()
    {
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
        catch (Exception)
        {
            return Task.FromResult<TestPlan?>(null);
        }
    }
}