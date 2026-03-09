using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Gebhardt.Acc.TestPlanRunner.Domain;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Provides JSON serialization options for test plans. To only serialize relevant and non null values for readability of the JSON content.
/// </summary>
[PublicAPI]
public static class TestPlanJsonOptions
{
    /// <summary>
    /// Creates JSON serializer options configured for writing test plans to test plan folder.
    /// </summary>
    public static JsonSerializerOptions CreateForPersistence()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            TypeInfoResolver = CreateTypeInfoResolver()
        };
    }

    private static DefaultJsonTypeInfoResolver CreateTypeInfoResolver()
    {
        return new DefaultJsonTypeInfoResolver
        {
            Modifiers = { ApplySerializationRules }
        };
    }

    private static void ApplySerializationRules(JsonTypeInfo typeInfo)
    {
        // Exclude computed / not needed properties
        if (typeInfo.Type == typeof(TestPlan))
        {
            string[] propertyNamesToExclude =
            [
                nameof(TestPlan.Name).ToLower(),
                nameof(TestPlan.LastWriteTime).ToLower(),
                nameof(TestPlan.AppliesToAllAgvs).ToLower(),
                nameof(TestPlan.HasInfiniteIterations).ToLower()
            ];

            List<JsonPropertyInfo> propertiesToRemove = typeInfo.Properties
                .Where(p => propertyNamesToExclude.Contains(p.Name.ToLower()))
                .ToList();

            foreach (var prop in propertiesToRemove)
            {
                typeInfo.Properties.Remove(prop);
            }
        }

        // Exclude null values for readability
        if (typeInfo.Type == typeof(TestPlanStep))
        {
            foreach (var property in typeInfo.Properties)
            {
                property.ShouldSerialize = (_, value) => value != null;
            }
        }
    }
}