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
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using MassTransit;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
	IHost? host = null;

	Parser.Default.ParseArguments<SimulateOptions, ProductionOptions>(args)
		.WithParsed<SimulateOptions>(simulationOptions =>
		{
			// Enable serialization/deserialization of enum values as string.
			SystemTextJsonMessageSerializer.Options.Converters.Add(new JsonStringEnumConverter());

			host = GebhardtStartup.CreateHostBuilder(args)
				.ConfigureServices((context, services) =>
				{
					services.AddScoped(_ => simulationOptions);
					services.AddTransient<SimulatedAgvProvider>();
					ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, 
						x =>
						{
							x.AddSagaStateMachine<SimulatedAgvStateMachine, SimulatedAgvSaga>()
								.InMemoryRepository();
						x.AddDelayedMessageScheduler();
					}, rabbitMqConfiguration: (_, cfg) =>
					{
						cfg.UseDelayedMessageScheduler();
					});
				})
				.Build();
			// Create AGVs
			host.Services.GetRequiredService<SimulatedAgvProvider>().Create();
		})
		.WithParsed<ProductionOptions>(productionOptions =>
		{
			host = GebhardtStartup.CreateHostBuilder(args)
				.ConfigureServices((context, services) =>
				{
					services.AddScoped(_ => productionOptions);
					ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration,
						busRegistrationConfiguration: configurator =>
						{
							//add consumers here
							configurator.AddMqttConsumer<AgvStateConsumer>();
							configurator.AddMqttConsumer<AgvGraphConsumer>();
							configurator.AddMqttConsumer<OrderEventConsumer>();
							configurator.AddConsumer<AgvRequestGraphConsumer>();
							configurator.AddConsumer<ExecuteTransportOrderConsumer>();
						},

						// ReSharper disable once UnusedParameter.Local
						rabbitMqConfiguration: (registrationContext, configurator) =>
						{
							//for outgoing MQTT messages the message topology needs to be configured
							configurator.ConfigureMessageTopology<MqttRequestGraph>();
							configurator.ConfigureMessageTopology<MqttOrder>();

							//for incoming MQTT messages the raw JSON serializer/deserializer needs to be added
							var serializerFactory = new SystemTextJsonRawMessageSerializerFactory();
							configurator.AddSerializer(serializerFactory);
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
