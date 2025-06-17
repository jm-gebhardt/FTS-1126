namespace Gebhardt.Acc.DeviceCommunication.Common.Configuration;

/// <summary>
/// Reads an instant action configuration file from yml.
/// </summary>
public class InstantActionsConfigurationFileReader: IConfigurationReader
{
    /// <inheritdoc cref="IConfigurationReader"/>>
    public TextReader? Read()
    {
        string? configPath = GetConfigFile();
        if (configPath == null)
        {
            return null;
        }
        return new StreamReader(configPath);
    }

    private static string? GetConfigFile()
    {
        const string configFile = "InstantActionsConfiguration.yml";
        const string configDirectory = "Configuration";
        string[] files = [];
        string? current = Path.Join(Directory.GetCurrentDirectory(), configDirectory);

        // Traverse the directory tree upwards to find a config file.
        while (files.Length == 0 && current != null)
        {
            files = Directory.GetFiles(current, configFile);
            current = Directory.GetParent(current)?.FullName ?? null;
        }

        if (files.Length == 0)
        {
            Console.WriteLine($"{configFile} could not be found.");
            return null;
        }

        return files.First();
    }
}