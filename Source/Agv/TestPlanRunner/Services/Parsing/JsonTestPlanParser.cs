using System.Text.Json;
using System.Text.Json.Serialization;
using Gebhardt.Acc.TestPlanRunner.Domain;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TestPlanRunner.Services.Parsing;

/// <summary>
/// Parser for JSON test plan files.
/// </summary>
[UsedImplicitly]
public class JsonTestPlanParser : ITestPlanParser
{
    private readonly ILogger<JsonTestPlanParser> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <inheritdoc cref="JsonTestPlanParser"/>
    public JsonTestPlanParser(ILogger<JsonTestPlanParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string[] SupportedExtensions => [".json"];

    /// <inheritdoc />
    public async Task<TestPlan?> ParseAsync(string content)
    {
        try
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            return await JsonSerializer.DeserializeAsync<TestPlan>(stream, _jsonOptions);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Error parsing JSON test plan");
            return null;
        }
    }
}