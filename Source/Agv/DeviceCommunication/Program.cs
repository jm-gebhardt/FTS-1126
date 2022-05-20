using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;


var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
	IHost? host = null;
	
	Parser.Default.ParseArguments<SimulateOptions, ProductionOptions>(args)
		.WithParsed<SimulateOptions>(simulationOptions =>
		{
			host = GebhardtStartup.CreateHostBuilder(args)
				.ConfigureServices((context, services) =>
				{
					services.AddScoped(_ => simulationOptions);
					ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
					{
						x.AddConsumer<SimulationExecuteTransportOrderConsumer>();
					});
				})
				.Build();
		})
		.WithParsed<ProductionOptions>(productionOptions =>
		{
			host = GebhardtStartup.CreateHostBuilder(args)
				.ConfigureServices((context, services) =>
				{
					services.AddScoped(_ => productionOptions);
					ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration);
				})
				.Build();
		});

	// By using the generic overload of WithParsed above host will be null for invalid command line arguments.
	if (host != null)
	{
		host.Run();
		return 0;
	}
	else
    {
		initialLogger.LogWarning("Invalid command line argument(s).");
		return 1;
    }
}
catch (Exception ex)
{
	initialLogger.LogError(ex, "Unhandled exception.");
	return 1;
}
