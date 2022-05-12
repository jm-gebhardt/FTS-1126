using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			IHost? host = null;
	
			Parser.Default.ParseArguments<SimulateOptions, ProductionOptions>(args)
				.WithParsed<SimulateOptions>(simulationOptions
					=>
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

			var logger = host?.Services.GetRequiredService<ILogger<Program>>();
			logger?.LogInformation("Starting program...");

			host?.Run();
			
			return 0;
        }
	}

}

