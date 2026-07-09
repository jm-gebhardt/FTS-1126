using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.Common.Utilities;

/// <summary>
/// Specifies the JSON naming policy to use when serializing and deserializing
/// the members of the annotated enum.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class JsonEnumNamingPolicyAttribute(JsonKnownNamingPolicy policy) : Attribute
{
    /// <summary>
    /// Resolves the JSON naming policy for the annotated enum.
    /// </summary>
    /// <returns>The resolved <see cref="JsonNamingPolicy"/> or <c>null</c> if unspecified.</returns>
    public JsonNamingPolicy? Resolve() => policy switch
    {
        JsonKnownNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
        JsonKnownNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
        JsonKnownNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
        _ => null // no attribute / Unspecified -> plain member names (current behavior)
    };
}

/// <summary>
/// A JSON converter factory that creates converters for enums annotated with
/// <see cref="JsonEnumNamingPolicyAttribute"/>.
/// </summary>
public sealed class AnnotatedEnumConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var policy = typeToConvert.GetCustomAttribute<JsonEnumNamingPolicyAttribute>()?.Resolve();
        return new JsonStringEnumConverter(policy).CreateConverter(typeToConvert, options);
    }
}
