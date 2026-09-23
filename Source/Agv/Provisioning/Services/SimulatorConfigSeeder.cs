using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.Provisioning.Configuration;
using Gebhardt.Acc.Provisioning.Models;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Provisioning.Services;

/// <inheritdoc />
[UsedImplicitly]
public class SimulatorConfigSeeder : JsonFolderSeeder
{
    private readonly IRequestClient<EnsureSimulatedAgvTypeConfiguration> _client;

    /// <inheritdoc />
    protected override string FolderName => "SimulatorConfig";

    /// <inheritdoc cref="SimulatorConfigSeeder"/>
    public SimulatorConfigSeeder(IOptions<ConfigOptions> configOptions, IRequestClient<EnsureSimulatedAgvTypeConfiguration> client, ILogger<SimulatorConfigSeeder> logger)
        : base(configOptions.Value.BasePath, logger)
    {
        _client = client;
    }

    /// <inheritdoc />
    protected override async Task SeedFileAsync(string filePath)
    {
        var config = DeserializeJson<SimulatorConfig>(await File.ReadAllTextAsync(filePath), filePath);
        if (string.IsNullOrWhiteSpace(config.AgvTypeIdentifier))
        {
            throw new InvalidOperationException($"No agv type identifier in simulator config {filePath}");
        }

        await _client.GetResponse<Success>(new EnsureSimulatedAgvTypeConfiguration(config.AgvTypeIdentifier.ToGuid(), config.AgvTypeIdentifier, config.Settings));

        Logger.LogInformation("Seeded simulation configuration of agv type {AgvTypeIdentifier}", config.AgvTypeIdentifier);
    }
}