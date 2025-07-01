using System.Collections;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Provisioning.Services;

/// <summary>
/// Seeds for json file.
/// </summary>
/// <typeparam name="TJson">The type of the json file.</typeparam>
public abstract class JsonSeeder<TJson> : ISeeder
{
    private readonly IBus _bus;
   
    /// <summary>
    /// The logger.
    /// </summary>
    [PublicAPI]
    protected readonly ILogger Logger;
    
    private readonly string _fileName = $"{typeof(TJson).Name}.json";
    private TJson? _content;

    // ReSharper disable once ConvertToConstant.Local Prepared for future use
    private readonly bool _skipNonExistentFile = true;

    /// <inheritdoc cref="AgvDataSeeder" />
    /// <param name="bus"></param>
    /// <param name="basePath">The full path where the json files are located.</param>
    /// <param name="logger"></param>
    protected JsonSeeder(IBus bus, string basePath, ILogger logger)
    {
        _bus = bus;
        BasePath = basePath;
        Logger = logger;
    }

    /// <inheritdoc cref="AgvDataSeeder" />
    protected JsonSeeder(IBus bus, string basePath, ILogger logger, string fileName)
    {
        _bus = bus;
        BasePath = basePath;
        Logger = logger;
        _fileName = fileName;
        logger.LogInformation("File name : {FileName}", _fileName);
    }

    private async Task<TJson> ReadJsonAsync(string path)
    {
        Logger.LogInformation("Read json data from {Path}", path);
        string json = await File.ReadAllTextAsync(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var content = JsonSerializer.Deserialize<TJson>(json, options);
        if (content == null)
        {
            Logger.LogError("Failed to deserialize json file {Path}", path);
            throw new InvalidOperationException($"Failed to deserialize json file {path}");
        }
        return content;
    }

    private string GetFilePath(string fileName) => Path.Combine(BasePath, fileName);

    /// <summary>
    /// The base folder for the files.
    /// </summary>
    protected string BasePath { get; }

    /// <summary>
    /// Validate the json data.
    /// </summary>
    /// <param name="data">The json data.</param>
    protected abstract void Validate(TJson data);

    /// <summary>
    /// Returns the message to be published on the bus.
    /// </summary>
    /// <param name="data">The data read from the json file.</param>
    /// <returns>The message to be published on the bus.</returns>
    protected abstract Task<object> GetMessageAsync(TJson data);

    /// <inheritdoc />
    public virtual string Name { get; } = typeof(TJson).Name;

    /// <inheritdoc />
    public virtual async Task SeedAsync()
    {
        string jsonFilePath = GetFilePath(_fileName);

        if (!File.Exists(jsonFilePath))
        {
            if (_skipNonExistentFile)
            {
                Logger.LogInformation("File {Path} not found, skipping", jsonFilePath);
                return;
            }

            throw new InvalidOperationException($"File {jsonFilePath} not found");
        }

        Logger.LogInformation("Seeding: {SeederName}", Name);

        _content = await ReadJsonAsync(jsonFilePath);

        Validate(_content);

        object message = await GetMessageAsync(_content);
        if (message is IList messages)
        {
            foreach (object? m in messages)
            {
                Logger.LogInformation("Sending message: {Message} of {MessageCount} messages", m, messages.Count);
                await _bus.Publish(m);
            }
            return;
        }
        Logger.LogInformation("Sending message: {Message}", message);
        await _bus.Publish(message);
    }

    /// <summary>
    /// Ensure the value of the property is not null.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="propertyName">The property name.</param>
    /// <exception cref="InvalidOperationException">Thrown, if the value is null.</exception>
    protected static void EnsureNotNull<T>(T? value, string propertyName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"{propertyName} is null.");
        }
    }
}
