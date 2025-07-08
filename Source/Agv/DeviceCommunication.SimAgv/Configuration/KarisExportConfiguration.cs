using JetBrains.Annotations;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Configuration;

/// <summary>
/// Provides a configuration for graphs exported from Karis
/// </summary>
[PublicAPI]
public class KarisExportConfiguration
{
    /// <summary>
    /// The directory where Karis export files are stored.
    /// </summary>
    public string? ExportDirectory { get; init; }
    
    private string _wayPointRegexPattern = "/";
    private string _chargeRegexPattern = "^(charge|or)(?!.*/).*$";
    private string _parkRegexPattern = "^(park)(?!.*/).*$";
    private string _pickRegexPattern = "^(pick)(?!.*/).*$";
    private string _dropRegexPattern = "^(drop)(?!.*/).*$";

    /// <summary>
    /// Regular expression pattern to identify waypoints by its node id.
    /// </summary>
    public string? WayPointRegexPattern
    {
        get => _wayPointRegexPattern;
        set
        {
            if (value != null)
            {
                _wayPointRegexPattern = value;
            }
            WayPointRegex = new Regex(_wayPointRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Regular expression pattern to identify charge stations by its node id.
    /// </summary>
    public string? ChargeRegexPattern
    {
        get => _chargeRegexPattern;
        set
        {
            if (value != null)
            {
                _chargeRegexPattern = value;
            }
            ChargeRegex = new Regex(_chargeRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Regular expression pattern to identify park stations by its node id.
    /// </summary>
    public string? ParkRegexPattern
    {
        get => _parkRegexPattern;
        set
        {
            if (value != null)
            {
                _parkRegexPattern = value;
            }
            ParkRegex = new Regex(_parkRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Regular expression pattern to identify pick stations by its node id.
    /// </summary>
    public string? PickRegexPattern
    {
        get => _pickRegexPattern;
        set
        {
            if (value != null)
            {
                _pickRegexPattern = value;
            }
            PickRegex = new Regex(_pickRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Regular expression pattern to identify drop stations by its node id.
    /// </summary>
    public string? DropRegexPattern
    {
        get => _dropRegexPattern;
        set
        {
            if (value != null)
            {
                _dropRegexPattern = value;
            }
            
            DropRegex = new Regex(_dropRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Regular expression to identify waypoints by its node id.
    /// </summary>
    public Regex WayPointRegex { get; private set; }

    /// <summary>
    /// Regular expression to identify charge stations by its node id.
    /// </summary>
    public Regex ChargeRegex { get; private set; }

    /// <summary>
    /// Regular expression to identify park stations by its node id.
    /// </summary>
    public Regex ParkRegex { get; private set; }

    /// <summary>
    /// Regular expression to identify pick stations by its node id.
    /// </summary>
    public Regex PickRegex { get; private set; }

    /// <summary>
    /// Regular expression to identify drop stations by its node id.
    /// </summary>
    public Regex DropRegex { get; private set; }

    /// <summary>
    /// If true, waypoints are renamed following "WP-XXXX".
    /// </summary>
    public bool? RenameWaypoints { get; set; } = true;

    /// <inheritdoc cref="KarisExportConfiguration"/>>
    public KarisExportConfiguration(string? exportDirectory)
    {
        WayPointRegex = new Regex(_wayPointRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        ChargeRegex = new Regex(_chargeRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        ParkRegex = new Regex(_parkRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        PickRegex = new Regex(_pickRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        DropRegex = new Regex(_dropRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        ExportDirectory = exportDirectory ?? Path.Combine("Resources", "KarisExport");
    }

    /// <inheritdoc cref="KarisExportConfiguration"/>>
    public KarisExportConfiguration()
    {
        WayPointRegex = new Regex(_wayPointRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        ChargeRegex = new Regex(_chargeRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        ParkRegex = new Regex(_parkRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        PickRegex = new Regex(_pickRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        DropRegex = new Regex(_dropRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        ExportDirectory =  Path.Combine("Resources", "KarisExport");
    }

    /// <summary>
    /// Load the configuration from a yaml file
    /// </summary>
    public void LoadFromYaml()
    {
        var deserializer = new DeserializerBuilder()
            .WithCaseInsensitivePropertyMatching()
            .Build();
        string path = Path.Combine(AppContext.BaseDirectory, ExportDirectory!, "ExportConfiguration.yaml");
        if (!Path.Exists(path))
        {
            Console.WriteLine($"Karis export configuration {path} not found, using default");
            return;
        }
        string yaml = File.ReadAllText(path);
        var config = deserializer.Deserialize<KarisExportConfiguration>(yaml);
        if (!string.IsNullOrEmpty(config.WayPointRegexPattern))
        {
            WayPointRegexPattern = config.WayPointRegexPattern;
        }
        if (!string.IsNullOrEmpty(config.ChargeRegexPattern))
        {
            ChargeRegexPattern = config.ChargeRegexPattern;
        }
        if(!string.IsNullOrEmpty(config.ParkRegexPattern))
        {
            ParkRegexPattern = config.ParkRegexPattern;
        }
        if (!string.IsNullOrEmpty(config.PickRegexPattern))
        {
            PickRegexPattern = config.PickRegexPattern;
        }
        if (!string.IsNullOrEmpty(config.DropRegexPattern))
        {
            DropRegexPattern = config.DropRegexPattern;
        }
        if (config.RenameWaypoints != null )
        {
            RenameWaypoints = config.RenameWaypoints;
        }
    }
}