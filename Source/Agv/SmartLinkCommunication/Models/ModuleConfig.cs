using JetBrains.Annotations;

namespace Gebhardt.Acc.SmartLinkCommunication.Models;

/// <summary>
/// The configuration of a SmartLink module with multiple IOs
/// </summary>
public class ModuleConfig
{
    /// <summary>
    /// The base Url of the module
    /// </summary>
    public string SmartLinkUrl
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The timeout for requests to the module [s]
    /// </summary>
    public int RequestTimeout
    {
        get;
        [UsedImplicitly]
        init;
    } = 5;

    /// <summary>
    /// A list of IO configurations. Here it is specified what IO will do what action under defined circumstances
    /// </summary>
    public List<IoActionConfig> IoActionConfigs
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The waiting time after a successful request
    /// </summary>
    public TimeSpan PollingInterval
    {
        get;
        [UsedImplicitly]
        init;
    } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Deserialization
    /// </summary>
    [UsedImplicitly]
    public ModuleConfig()
    {
        SmartLinkUrl = null!;
        IoActionConfigs = null!;
    }

    /// <inheritdoc cref="ModuleConfig"/>
    /// <param name="smartLinkUrl">The base Url of the module</param>
    /// <param name="ioActionConfigs">A list of IO configurations. Here it is specified what IO will do what action under defined circumstances</param>
    public ModuleConfig(string smartLinkUrl, List<IoActionConfig> ioActionConfigs)
    {
        SmartLinkUrl = smartLinkUrl;
        IoActionConfigs = ioActionConfigs;
    }
}