using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using MassTransit.Configuration;
using MassTransit.Serialization;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;


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
			//Add CamelCase converter to standard JSOnSerializer to read MQTT messages correctly
			SystemTextJsonMessageSerializer.Options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

			host = GebhardtStartup.CreateHostBuilder(args)
				.ConfigureServices((context, services) =>
				{
					services.AddScoped(_ => productionOptions);
					ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration,
						busRegistrationConfiguration: configurator =>
						{
							//add consumers here
							configurator.AddMqttConsumer<AgvStateConsumer>();
							configurator.AddConsumer<ExecuteTransportOrderConsumer>();
						},

						// ReSharper disable once UnusedParameter.Local
						rabbitMqConfiguration: (registrationContext, configurator) =>
						{
							//for outgoing MQTT messages the message topology needs to be configured
							configurator.ConfigureMessageTopology<IOrder>();

							//for incoming MQTT messages the raw JSON serializer/deserializer needs to be added
							var serializerFactory = new SystemTextJsonRawMessageSerializerFactory();
							configurator.AddSerializer(serializerFactory);
							configurator.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
						});
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
		initialLogger.LogWarning("Invalid command line argument(s)");
		return 1;
	}
}
catch (Exception ex)
{
	initialLogger.LogError(ex, "Unhandled exception");
	return 1;
}
