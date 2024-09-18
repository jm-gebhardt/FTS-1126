using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.SmartLinkCommunication;
using Gebhardt.Acc.SmartLinkCommunication.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp;

var initialLogger = InitialLogger.Create<Program>();

try
{
    var config = await CmdParser.ParseAsync(args, initialLogger);

    if (config == null)
    {
        initialLogger.LogError("Invalid command line argument(s)");
        return 1;
    }

    var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddScoped<IIoActionExecutionService, IoActionExecutionService>();
            services.AddScoped<IRestClient, RestClient>();
            services.AddScoped<ISmartLinkService, SmartLinkService>();
            services.AddScoped<ISmartLinkResponseHandler, SmartLinkResponseHandler>();
            services.AddScoped<IIoStateTrackerService, IoStateTrackerService>();
            services.AddSingleton<IModuleScopeFactory, ModuleScopeFactory>();
            services.AddScoped<IModuleConfigProvider, ModuleConfigProvider>();

            foreach (var moduleConfig in config.Modules)
            {
                // we must construct the PollingBackgroundService manually, as there is no other way to inject  this specific instance of the moduleConfig
                services.AddHostedService(sp => new PollingBackgroundService<ModulePollingService>(
                    sp.GetRequiredService<ILogger<PollingBackgroundService<ModulePollingService>>>(),
                    new ModulePollingService(sp, moduleConfig)
                ));
            }

            services.ConfigureServicesMassTransit(context.Configuration);
        });

    var host = hostBuilder.Build();

    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}