using System.Reflection;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using static System.String;

namespace Gebhardt.Acc.Common;

/// <summary>
/// Encapsulates startup procedure.
/// </summary>
[PublicAPI]
public static class GebhardtStartup
{
    /// <summary>
    /// Check if we are running in Docker Container
    /// </summary>
    public static bool InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    /// <summary>
    /// Create default host and create singleton worker implementation.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    [PublicAPI]
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        string? serviceName = entryAssembly?.GetName().Name;
        if (IsNullOrEmpty(serviceName))
        {
            throw new InvalidOperationException("Service name information missing.");
        }

        var host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options => { options.ServiceName = serviceName; })
            .ConfigureServices(services => { services.ConfigureEventLogSettings(entryAssembly); })
            .ConfigureWebHost(host =>
            {
                host.UseKestrel()
                    .Configure(app => { app.UseMetricServer(); });
            })
            .UseDefaultServiceProvider((_, o) =>
            {
                o.ValidateOnBuild = true;
                o.ValidateScopes = true;
            })
            .ConfigureLogging(conf =>
            {
                // Use json output when running in Docker
                if (InDocker)
                {
                    conf.AddJsonConsole();
                }
                else
                {
                    conf.AddConsole(options => { options.FormatterName = "simple"; }).AddSimpleConsole(options => { options.TimestampFormat = "[HH:mm:ss.fff] "; });
                }
                // Disable info log for WebServer
                conf.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            });
        return host;
    }
}