using System.Text.Json;
using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Exceptions;

namespace Gebhardt.Acc.Common.Filter.Api.Swagger;

/// <inheritdoc />
public class FilterJsonConverter : JsonConverter<FilterBaseDto>
{
    /// <inheritdoc/>
    public override FilterBaseDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader); 
        var root = document.RootElement;

        if (root.TryGetProperty(nameof(CollectionFilterDto.Aggregation), out _))
        {
            return root.Deserialize<CollectionFilterDto>(options);
        }

        if (root.TryGetProperty(nameof(CombinationFilterDto.Combination), out _))
        {
            return root.Deserialize<CombinationFilterDto>(options);
        }

        if (root.TryGetProperty(nameof(ValueComparisonFilterDto.Variant), out _))
        {
            return root.Deserialize<ValueComparisonFilterDto>(options);
        }

        throw new FilterBuilderException($"Could not deserialize {nameof(FilterBaseDto)}: {root}");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, FilterBaseDto value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case CollectionFilterDto coll:
                JsonSerializer.Serialize(writer, coll, options);
                break;

            case CombinationFilterDto comb:
                JsonSerializer.Serialize(writer, comb, options);
                break;

            case ValueComparisonFilterDto comp:
                JsonSerializer.Serialize(writer, comp, options);
                break;

            default:
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract Could be streamed in as null from unsafe code.
                throw new FilterBuilderException($"Unknown filter type {value?.GetType().Name}");
        }
    }
}