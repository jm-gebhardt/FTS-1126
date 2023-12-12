using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventLog;

namespace Gebhardt.StoreWare.Agv.Common.Utilities;

/// <summary>
/// Helper class for handling EventLog (Windows)
/// </summary>
public static class EventLogHelper
{
    [SupportedOSPlatform("windows")]
    private static void ConfigureEventLogSettingsForWindows(IServiceCollection services, Assembly? executingAssembly)
    {
        services.Configure<EventLogSettings>(settings => { settings.SourceName = executingAssembly?.GetName().Name; });
    }

    /// <summary>
    /// Set the SourceName of the event log to the executing assembly name.<br/>
    /// Whereby this only applies to Windows
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="executingAssembly">The executing assembly</param>
    public static void ConfigureEventLogSettings(IServiceCollection services, Assembly? executingAssembly)
    {
        if (OperatingSystem.IsWindows())
        {
            ConfigureEventLogSettingsForWindows(services, executingAssembly);
        }
    }
}