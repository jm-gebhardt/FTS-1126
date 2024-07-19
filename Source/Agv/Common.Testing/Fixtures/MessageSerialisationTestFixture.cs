using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

public class MessageSerialisationTestFixture
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public MessageSerialisationTestFixture()
    {
        _options.Converters.Add(new JsonStringEnumConverter());
    }

    public void ValidateSerialization<TMessage>(TMessage message)
        where TMessage : class
    {
        string serializedMessage = JsonSerializer.Serialize(message);
        var deserializedMessage = JsonSerializer.Deserialize<TMessage>(serializedMessage, _options);

        deserializedMessage.ShouldBeEquivalentTo(message);
    }
}