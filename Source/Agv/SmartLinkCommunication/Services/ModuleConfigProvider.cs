using Gebhardt.Acc.SmartLinkCommunication.Exceptions;
using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

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