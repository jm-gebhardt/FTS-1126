using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Exceptions;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;

/// <inheritdoc />
public class ModuleConfigProvider : IModuleConfigProvider
{
    private ModuleConfig? _moduleConfig;

    /// <inheritdoc />
    public ModuleConfig ModuleConfig => _moduleConfig ?? throw new ModuleConfigScopeNotSetException();

    /// <inheritdoc />
    public void SetModuleConfig(ModuleConfig moduleConfig)
    {
        _moduleConfig = moduleConfig;
    }
}