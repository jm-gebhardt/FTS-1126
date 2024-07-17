using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserConveyorTelegramApiServer.Configuration;
using Gebhardt.Acc.KaeserConveyorTelegramApiServer.Services;
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
            services.AddHostedService<CommunicationManager>();
            var config = new TelegramCommunicationConfiguration();
            config.LoadFromConfig(context.Configuration.GetRequiredSection("CONVEYOR"));
            services.AddSingleton(config);
            services.AddSingleton<Server>();
            services.AddSingleton<ConveyorTelegramProcessor>();
            services.AddSingleton<Receiver<ConveyorTelegram>>(serviceProvider =>
            {
                var processor = serviceProvider.GetRequiredService<ConveyorTelegramProcessor>();
                var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<ConveyorTelegram>>>();
                var socket = serviceProvider.GetRequiredService<Server>();
                return new Receiver<ConveyorTelegram>(socket, "conveyorReceiver", processor, logger);
            });
        });
    var host = hostBuilder.Build();
    host.Run();
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
}