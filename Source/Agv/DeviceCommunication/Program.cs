using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CommandLine;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			IHost? host = null;
	
			Parser.Default.ParseArguments<SimulateOptions, ProductionOptions>(args)
				.WithParsed<SimulateOptions>(simulationOptions
					=>
				{
					host = Common.GebhardtStartup.CreateHostBuilder(args)
						.ConfigureServices((context, services) =>
						{
							services.AddScoped(_ => simulationOptions);
							// Configure MassTransit with RabbitMQ if the worker requires it
							Common.ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
							{
								x.AddConsumer<Consumers.ExecuteTransportOrderConsumerSim>();
							});
						})
						.Build();
				})
				.WithParsed<ProductionOptions>(productionOptions =>
				{
					host = Common.GebhardtStartup.CreateHostBuilder(args)
						.ConfigureServices((context, services) =>
						{
							services.AddScoped(_ => productionOptions);
							// Configure MassTransit with RabbitMQ if the worker requires it
							Common.ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration);
						})
						.Build();
				});

			host?.Run();
			
			return 0;
        }
	}

}

