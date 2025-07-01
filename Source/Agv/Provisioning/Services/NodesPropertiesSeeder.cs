using Gebhardt.Acc.Provisioning.Configuration;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Provisioning.Services;

/// <summary>
/// Seeder for nodes properties
/// </summary>
public class NodesPropertiesSeeder: ISeeder
{
    private readonly IOptions<ConfigOptions> _configOptions;
    private readonly IBus _bus;
    private readonly ILogger<NodesPropertiesSeeder> _logger;
    private readonly string _nodeFolder;
    private string _fileName = null!;
    private readonly ILogger<NodePropertiesSeeder> _nodePropertiesLogger;

    /// <inheritdoc cref="NodesPropertiesSeeder"/>
    public NodesPropertiesSeeder(IOptions<ConfigOptions> configOptions,
        ILogger<NodesPropertiesSeeder> logger,
        ILogger<NodePropertiesSeeder> nodePropertiesLogger,
        IBus bus)
    {
        Name = "Nodes seeder";
        _configOptions = configOptions;
        _logger = logger;
        _nodePropertiesLogger = nodePropertiesLogger;
        _bus = bus;
        _nodeFolder = GetPath();
    }

    private string GetPath()
    {
        string basePath = Path.Combine(_configOptions.Value.BasePath, "NodeProperties");

        if (!Directory.Exists(basePath))
        {
            _logger.LogInformation("Directory {BasePath} not found.", basePath);
            return "";
        }
        _fileName = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories).Select(Path.GetFileName).Single() ?? throw new InvalidOperationException("No file found");

        _logger.LogInformation("Node properties file: {FileName}", _fileName);

        return basePath;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        await new NodePropertiesSeeder(_bus, _nodeFolder, _nodePropertiesLogger, _fileName).SeedAsync();
    }
}
