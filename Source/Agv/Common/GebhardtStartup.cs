using System.Reflection;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
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

        if (!InDocker && Environment.UserInteractive && !Console.IsOutputRedirected)
        {
            Console.Title = GetAfterSecondDot(serviceName);
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
            .ConfigureLogging((context, conf) =>
            {
                // Use json output when running in Docker
                if (InDocker)
                {
                    conf.AddJsonConsole();
                }
                else
                {
                    conf.AddConsole(options => { options.FormatterName = "simple"; }).AddSimpleConsole(options => { options.TimestampFormat = "[HH:mm:ss.fff] "; });
                    AddFileLogging(conf, context.Configuration, serviceName);
                }
                // Disable info log for WebServer
                conf.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            });
        return host;
    }

    private static void AddFileLogging(ILoggingBuilder logging, IConfiguration configuration, string serviceName)
    {
        string? directory = configuration["Logging:File:Path"];
        if (IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(AppContext.BaseDirectory, "logs");
        }

        string logFilePath = Path.Combine(directory, $"{serviceName}.log");
        ResetLogFileIfFromPreviousDay(logFilePath);

        // The file is filtered by Serilog only: Information and above for Gebhardt.Acc namespaces, Error and above for everything else.
        var fileLogger = new LoggerConfiguration()
            .MinimumLevel.Error()
            .MinimumLevel.Override("Gebhardt.Acc", LogEventLevel.Information)
            .WriteTo.File(
                logFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                shared: true)
            .CreateLogger();

        logging.AddSerilog(fileLogger, dispose: true);

        // Let everything through the Microsoft logging filters (including Logging:LogLevel from appsettings) so that only the Serilog rules above apply.
        logging.AddFilter<SerilogLoggerProvider>(null, LogLevel.Trace);
    }

    // Overwrite the fixed-name log file once per day: it is cleared on startup if it was last written before today.
    private static void ResetLogFileIfFromPreviousDay(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (File.Exists(path) && File.GetLastWriteTime(path).Date < DateTime.Today)
            {
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            }
        }
        catch (IOException)
        {
            // File is locked by another process; keep appending to it.
        }
    }

    private static string GetAfterSecondDot(string input)
    {
        if (IsNullOrWhiteSpace(input))
        {
            return input;
        }

        string[] parts = input.Split('.');

        return parts.Length > 2 ? Join(".", parts.Skip(2)) : input; 
    }
}