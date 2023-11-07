using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;

/// <inheritdoc />
public class ModuleScopeFactory : IModuleScopeFactory
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    /// <inheritdoc cref="ModuleScopeFactory"/>
    /// <param name="serviceScopeFactory"></param>
    public ModuleScopeFactory(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc />
    public IServiceScope CreateScope(ModuleConfig moduleConfig)
    {
        var scope = _serviceScopeFactory.CreateScope();
        var moduleConfigProvider = scope.ServiceProvider.GetRequiredService<IModuleConfigProvider>();
        moduleConfigProvider.SetModuleConfig(moduleConfig);
        return scope;
    }
}