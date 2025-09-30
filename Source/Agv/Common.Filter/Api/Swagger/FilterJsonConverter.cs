using System.Text.Json;
using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Exceptions;

namespace Gebhardt.Acc.Common.Filter.Api.Swagger;

/// <inheritdoc />
public class FilterJsonConverter : JsonConverter<FilterDto>
{
    /// <inheritdoc/>
    public override FilterDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        throw new FilterDtoParserException($"Could not deserialize {nameof(FilterDto)}: {root}");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, FilterDto value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case CollectionFilterDto collectionFilterDto:
                JsonSerializer.Serialize(writer, collectionFilterDto, options);
                break;

            case CombinationFilterDto combinationFilterDto:
                JsonSerializer.Serialize(writer, combinationFilterDto, options);
                break;

            case ValueComparisonFilterDto valueComparisonFilterDto:
                JsonSerializer.Serialize(writer, valueComparisonFilterDto, options);
                break;

            default:
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract Could be streamed in as null from unsafe code.
                throw new FilterDtoParserException($"Unknown filter type {value?.GetType().Name}");
        }
    }
}