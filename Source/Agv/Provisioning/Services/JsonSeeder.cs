using MassTransit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Gebhardt.StoreWare.Agv.Provisioning.Services;

/// <summary>
/// Seeds for json file.
/// </summary>
/// <typeparam name="TJson">The type of the json file.</typeparam>
internal abstract class JsonSeeder<TJson> : ISeeder
{
    private readonly IBus _bus;
    private readonly ILogger _logger;
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
        _logger = logger;
    }

    private async Task<TJson> ReadJsonAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        var content = JsonConvert.DeserializeObject<TJson>(json);

        if (content == null)
        {
            _logger.LogError("Failed to deserialize json file {Path}", path);
            throw new InvalidOperationException($"Failed to deserialize json file {path}");
        }
        return content;
    }

    private string GetFilePath(string fileName) => Path.Combine(BasePath, fileName);

    /// <summary>
    /// The base folder for the files.
    /// </summary>
    protected string BasePath { get; }

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
                _logger.LogDebug("File {Path} not found, skipping", jsonFilePath);
                return;
            }

            throw new InvalidOperationException($"File {jsonFilePath} not found");
        }

        _logger.LogInformation("Seeding: {SeederName}", Name);

        _content = await ReadJsonAsync(jsonFilePath);

        Validate(_content);

        object message = await GetMessageAsync(_content);
        await _bus.Publish(message);
    }

    protected static void EnsureNotNull<T>(T? value, string propertyName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"{propertyName} is null.");
        }
    }
}