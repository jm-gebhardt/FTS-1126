using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
public class AgvSeeder : ISeeder
{
    private const int WaitingTimeMs = 1000;
    private readonly AgvStatusSeeder _agvStatusSeeder;
    private readonly AgvGraphSeeder _agvGraphSeeder;
    private readonly AgvDataSeeder _agvDataSeeder;
    private readonly string _folderName;
    private readonly ILogger<AgvSeeder> _logger;

    /// <inheritdoc />
    public string Name { get; } = nameof(AgvSeeder);

    /// <inheritdoc cref="AgvSeeder"/>
    public AgvSeeder(
        AgvStatusSeeder agvStatusSeeder,
        AgvGraphSeeder agvGraphSeeder,
        AgvDataSeeder agvDataSeeder,
        string folderName,
        ILogger<AgvSeeder> logger
    )
    {
        _agvStatusSeeder = agvStatusSeeder;
        _agvGraphSeeder = agvGraphSeeder;
        _agvDataSeeder = agvDataSeeder;
        _folderName = folderName;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        _logger.LogInformation("Seeding______________________{FolderName}____________________________", _folderName);
        await _agvStatusSeeder.SeedAsync();
        // just wait for some time until the next seeder publishes its data, so that the order is right.
        await Task.Delay(WaitingTimeMs);
        await _agvGraphSeeder.SeedAsync();
        await Task.Delay(WaitingTimeMs);
        await _agvDataSeeder.SeedAsync();
        _logger.LogInformation("_____________________________{FolderName}________________________Done", _folderName);
    }
}