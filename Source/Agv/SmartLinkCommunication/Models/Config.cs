using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

/// <summary>
/// The system configuration
/// </summary>
public class Config
{
    /// <summary>
    /// The configured SmartLink modules
    /// </summary>
    public List<ModuleConfig> Modules
    {
        get;
        [UsedImplicitly]
        set;
    }

    /// <summary>
    /// Deserialization
    /// </summary>
    [UsedImplicitly]
    public Config()
    {
        Modules = null!;
    }

    /// <inheritdoc cref="Config"/>
    /// <param name="modules"></param>
    public Config(List<ModuleConfig> modules)
    {
        Modules = modules;
    }
}