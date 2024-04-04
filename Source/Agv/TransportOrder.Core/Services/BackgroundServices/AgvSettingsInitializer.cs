using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service to initialize the agv settings.
/// </summary>
/// <param name="agvSettingsProvider"></param>
public class AgvSettingsInitializer(IAgvSettingsProvider agvSettingsProvider): BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await agvSettingsProvider.InitializeSettingsGroups();
    }
}