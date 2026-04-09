using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;

/// <summary>
/// Deserializes the polymorphic <see cref="BackgroundObjectDto"/> hierarchy by peeking at
/// the <c>objectType</c> property before selecting the concrete target type.
/// Registered via <c>[JsonConverter]</c> on <see cref="BackgroundObjectDto"/>.
/// </summary>
internal sealed class BackgroundObjectDtoConverter : JsonConverter<BackgroundObjectDto>
{
    public override BackgroundObjectDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("objectType", out var prop) ||
            !Enum.TryParse<ObjectTypeDto>(prop.GetString(), ignoreCase: true, out var objectType))
        {
            throw new JsonException($"Missing or unknown objectType '{prop.GetString()}'.");
        }

        var concreteType = objectType switch
        {
            ObjectTypeDto.Rectangle => typeof(RectangleObjectDto),
            ObjectTypeDto.Triangle  => typeof(TriangleObjectDto),
            ObjectTypeDto.Circle    => typeof(CircleObjectDto),
            ObjectTypeDto.Line      => typeof(LineObjectDto),
            ObjectTypeDto.Polyline  => typeof(PolylineObjectDto),
            ObjectTypeDto.Text      => typeof(TextObjectDto),
            _                       => throw new JsonException($"Unhandled objectType '{objectType}'.")
        };

        return (BackgroundObjectDto)JsonSerializer.Deserialize(root.GetRawText(), concreteType, options)!;
    }

    public override void Write(Utf8JsonWriter writer, BackgroundObjectDto value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
