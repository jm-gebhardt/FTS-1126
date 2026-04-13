using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.BackgroundServices;

/// <summary>
/// Provides a background service for initializing prismat communication.
/// </summary>
public class InitializationProvider(ILogger<InitializationProvider> logger, IAgvRepository agvRepository): BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Initializing..");
        await agvRepository.InitializeCacheAsync();
    }
}