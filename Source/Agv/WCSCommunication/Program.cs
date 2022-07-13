using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
    IHost? host = null;
    
    Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
        // Simulation Mode
        .WithParsed<SimulateOptions>(simulationOptions =>
        {
            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
					services.AddSingleton(_ => simulationOptions);
                    services.AddHostedService<PollingBackgroundService<SimulationHostOrderService>>();
                    services.AddSingleton<SimulationHostOrderService>();
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                    {
	                    x.AddConsumer<SimulationConfirmHostOrderConsumer>();
                    });
                }).Build();
            host.Run();
        })
        // Production Mode
        .WithParsed<ProductionOptions>( _ => {
            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
            {
                services.AddDbContextFactory<WcsDbContext>(builder =>
                {
	                builder.UseSqlServer(context.Configuration.GetConnectionString(WcsDbContext.ConfigSectionName));
                });
                services.AddHostedService<PollingBackgroundService<WcsOrderHandler>>();
                services.AddSingleton<WcsOrderHandler>();
                ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                {
                    x.AddConsumer<WcsHostOrderConsumer>();
                });
            })
            .Build();
        });
    // By using the generic overload of WithParsed above host will be null for invalid command line arguments.
    if (host != null)
    {
	    using (var scope = host.Services.CreateScope())
	    {
		    var db = scope.ServiceProvider.GetService<WcsDbContext>();
		    db?.Database.Migrate();
	    }
        host.Run();
        return 0;
    }
    else
    {
        initialLogger.LogWarning("Invalid command line argument(s)");
        return 1;
    }
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}
