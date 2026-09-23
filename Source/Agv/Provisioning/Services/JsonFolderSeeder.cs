using System.Text.Json;
using Gebhardt.Acc.Common.Utilities;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Provisioning.Services;

/// <summary>
/// Seeds every json file of one folder below the provisioning base path.
/// </summary>
public abstract class JsonFolderSeeder : ISeeder
{
    private string FolderPath => Path.Combine(field, FolderName);

    /// <summary>
    /// The name of the folder below the provisioning base path.
    /// </summary>
    protected abstract string FolderName { get; }

    /// <summary>
    /// The logger.
    /// </summary>
    [PublicAPI]
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public string Name => $"{FolderName} seeder";

    /// <inheritdoc cref="JsonFolderSeeder"/>
    /// <param name="basePath">The full path where the provisioning data is located.</param>
    /// <param name="logger">The logger.</param>
    protected JsonFolderSeeder(string basePath, ILogger logger)
    {
        FolderPath = basePath;
        Logger = logger;
    }

    /// <summary>
    /// Deserializes the content of a json file.
    /// </summary>
    /// <typeparam name="TJson">The type the file contains.</typeparam>
    /// <param name="json">The content of the file.</param>
    /// <param name="filePath">The full path of the file.</param>
    /// <returns>The deserialized content.</returns>
    /// <exception cref="InvalidOperationException">Thrown, if the content cannot be deserialized.</exception>
    protected static TJson DeserializeJson<TJson>(string json, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new AnnotatedEnumConverterFactory() }
        };

        return JsonSerializer.Deserialize<TJson>(json, options) ?? throw new InvalidOperationException($"Failed to deserialize json file {filePath}");
    }

    /// <summary>
    /// Seeds a single json file.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    protected abstract Task SeedFileAsync(string filePath);

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        string folderPath = FolderPath;
        if (!Directory.Exists(folderPath))
        {
            Logger.LogInformation("Directory {FolderPath} not found, skipping", folderPath);
            return;
        }

        string[] filePaths = Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories).Order().ToArray();
        if (filePaths.Length == 0)
        {
            Logger.LogInformation("No json files in {FolderPath}, skipping", folderPath);
            return;
        }

        foreach (string filePath in filePaths)
        {
            Logger.LogInformation("Seeding {FilePath}", filePath);
            await SeedFileAsync(filePath);
        }
    }
}