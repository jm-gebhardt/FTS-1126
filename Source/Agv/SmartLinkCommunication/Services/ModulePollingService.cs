using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;

/// <summary>
/// Fetches data from SmartLinkService and processes them
/// </summary>
public class ModulePollingService : IPollable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ModuleConfig _moduleConfig;

    /// <inheritdoc />
    public int PollingInterval => (int)_moduleConfig.PollingInterval.TotalMilliseconds;

    /// <inheritdoc cref="ModulePollingService" />
    /// <param name="serviceProvider"></param>
    /// <param name="moduleConfig"></param>
    public ModulePollingService(IServiceProvider serviceProvider, ModuleConfig moduleConfig)
    {
        _serviceProvider = serviceProvider;
        _moduleConfig = moduleConfig;
    }

    /// <inheritdoc />
    public async Task Poll(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.GetRequiredService<IModuleScopeFactory>().CreateScope(_moduleConfig);
        var serviceProvider = scope.ServiceProvider;
        var smartLinkService = serviceProvider.GetRequiredService<ISmartLinkService>();
        var responseHandler = serviceProvider.GetRequiredService<ISmartLinkResponseHandler>();

        var response = await smartLinkService.GetDataAsync();
        await responseHandler.HandleAsync(response);
    }
}