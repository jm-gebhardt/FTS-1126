using System.Text.Json;
using System.Text.Json.Serialization;
using Gebhardt.Acc.TestPlanRunner.Domain;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Services.Parsing;

/// <summary>
/// Parser for JSON test plan files.
/// </summary>
[UsedImplicitly]
public class JsonTestPlanParser : ITestPlanParser
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip
    };

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
        catch (JsonException)
        {
            return null;
        }
    }
}
