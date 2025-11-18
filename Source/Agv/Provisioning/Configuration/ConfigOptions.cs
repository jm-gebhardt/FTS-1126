namespace Gebhardt.Acc.Provisioning.Configuration;

/// <summary>
/// The configuration options for the provisioning.
/// </summary>
public class ConfigOptions
{
    /// <summary>
    /// The name of the section in the appsettings.json file.
    /// </summary>
    public const string SectionName = "Config";

    /// <summary>
    /// The base path for the provisioning data.
    /// </summary>
    public string BasePath
    {
        get => field ?? throw new InvalidOperationException($"{nameof(BasePath)} not set");
        init;
    }
}