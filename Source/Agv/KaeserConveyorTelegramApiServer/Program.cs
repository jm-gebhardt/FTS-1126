using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserConveyorTelegramApiServer.Configuration;
using Gebhardt.Acc.KaeserConveyorTelegramApiServer.Services;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();
try
{
    var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
        .ConfigureServices((context, services) =>
        {
            // Configuration
            var aplWeStationConfig = new TelegramCommunicationConfiguration();
            aplWeStationConfig.LoadFromConfig(context.Configuration.GetRequiredSection(ServiceKeyProvider.AplWeStation));
            services.AddKeyedSingleton(ServiceKeyProvider.AplWeStation, (_, _) => aplWeStationConfig);

            var agvCargoStateConfig = new TelegramCommunicationConfiguration();
            agvCargoStateConfig.LoadFromConfig(context.Configuration.GetRequiredSection(ServiceKeyProvider.AgvCargoState));
            services.AddKeyedSingleton(ServiceKeyProvider.AgvCargoState, (_, _) => agvCargoStateConfig);

            // Servers
            services.AddKeyedSingleton<Server>(ServiceKeyProvider.AplWeStation, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SocketBase>>();
                return new Server(logger, aplWeStationConfig);
            });

            services.AddKeyedSingleton<Server>(ServiceKeyProvider.AgvCargoState, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SocketBase>>();
                return new Server(logger, agvCargoStateConfig);
            });

            // Telegram processors
            services.AddKeyedSingleton<ConveyorTelegramProcessor>(ServiceKeyProvider.AplWeStation,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ConveyorTelegramProcessor>>();
                    return new ConveyorTelegramProcessor(logger, aplWeStationConfig);
                });

            services.AddKeyedSingleton<ConveyorTelegramProcessor>(ServiceKeyProvider.AgvCargoState,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ConveyorTelegramProcessor>>();
                    return new ConveyorTelegramProcessor(logger, agvCargoStateConfig);
                });

            // Receiver
            services.AddKeyedSingleton<Receiver<ConveyorTelegram>>(ServiceKeyProvider.AgvCargoState,
                (serviceProvider, _) =>
            {
                var processor = serviceProvider.GetKeyedService<ConveyorTelegramProcessor>(ServiceKeyProvider.AgvCargoState);
                var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<ConveyorTelegram>>>();
                var socket = serviceProvider.GetKeyedService<Server>(ServiceKeyProvider.AgvCargoState);
                return new Receiver<ConveyorTelegram>(socket!, ServiceKeyProvider.AgvCargoState, processor!, logger);
            });

            services.AddKeyedSingleton<Receiver<ConveyorTelegram>>(ServiceKeyProvider.AplWeStation,
                (serviceProvider, _) =>
                {
                    var processor = serviceProvider.GetKeyedService<ConveyorTelegramProcessor>(ServiceKeyProvider.AplWeStation);
                    var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<ConveyorTelegram>>>();
                    var socket = serviceProvider.GetKeyedService<Server>(ServiceKeyProvider.AplWeStation);
                    return new Receiver<ConveyorTelegram>(socket!, ServiceKeyProvider.AplWeStation, processor!, logger);
                });

            services.AddHostedService<AplWeStationCommunicationManager>();
            services.AddHostedService<AgvCargoStateCommunicationManager>();
        });
    var host = hostBuilder.Build();
    host.Run();
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
}