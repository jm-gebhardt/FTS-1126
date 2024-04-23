using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

/// <summary>
/// Provides a <see cref="ModuleConfig"/> instance
/// </summary>
public interface IModuleConfigProvider
{
    /// <summary>
    /// The <see cref="ModuleConfig"/> instance
    /// </summary>
    ModuleConfig ModuleConfig { get; }

    /// <summary>
    /// Sets the <see cref="ModuleConfig"/> instance
    /// </summary>
    /// <param name="moduleConfig"></param>
    void SetModuleConfig(ModuleConfig moduleConfig);
}