using Gebhardt.Acc.SmartLinkCommunication.Exceptions;
using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

/// <inheritdoc />
public class IoStateTrackerService : IIoStateTrackerService
{
    private readonly IModuleConfigProvider _moduleConfigProvider;
    private static readonly Dictionary<ModuleConfig, Dictionary<IoActionConfig, IoStateTracker>> _dictionaryByModuleConfig = new();

    /// <inheritdoc />
    public Dictionary<IoActionConfig, IoStateTracker> IoStateTrackers
    {
        get
        {
            if (_dictionaryByModuleConfig.TryGetValue(_moduleConfigProvider.ModuleConfig, out var dictionary))
            {
                return dictionary;
            }

            throw new ModuleConfigScopeNotSetException();
        }
    }

    /// <inheritdoc cref="IoStateTrackerService"/>
    /// <param name="moduleConfigProvider"></param>
    public IoStateTrackerService(IModuleConfigProvider moduleConfigProvider)
    {
        _moduleConfigProvider = moduleConfigProvider;
        _dictionaryByModuleConfig.TryAdd(moduleConfigProvider.ModuleConfig, new Dictionary<IoActionConfig, IoStateTracker>());
    }
}