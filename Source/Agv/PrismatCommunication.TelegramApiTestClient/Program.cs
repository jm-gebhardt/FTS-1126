using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunication.TelegramApiClient.Configuration;
using Gebhardt.Acc.PrismatCommunication.TelegramApiClient.Services;
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
            var toFtsConfig = new TelegramCommunicationConfiguration();
            toFtsConfig.LoadFromConfig(context.Configuration.GetRequiredSection(ServiceKeyProvider.ToFts));
            var fromFtsConfig = new TelegramCommunicationConfiguration();
            fromFtsConfig.LoadFromConfig(context.Configuration.GetRequiredSection(ServiceKeyProvider.FromFts));
            services.AddKeyedSingleton<Client>(ServiceKeyProvider.ToFts, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Client>>();
                return new Client(logger, toFtsConfig);
            });
            services.AddKeyedSingleton<Client>(ServiceKeyProvider.FromFts, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Client>>();
                return new Client(logger, fromFtsConfig);
            });
            services.AddKeyedSingleton<PrismatTelegramProcessor>(ServiceKeyProvider.ToFts,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<PrismatTelegramProcessor>>();
                    return new PrismatTelegramProcessor(logger, false, toFtsConfig);
                });
            services.AddKeyedSingleton<PrismatTelegramProcessor>(ServiceKeyProvider.FromFts,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<PrismatTelegramProcessor>>();
                    return new PrismatTelegramProcessor(logger, true, fromFtsConfig);
                });

            services.AddSingleton(serviceProvider =>
            {
                var toFtsProcessor = serviceProvider.GetKeyedService<PrismatTelegramProcessor>(ServiceKeyProvider.ToFts);
                var socket = serviceProvider.GetKeyedService<Client>(ServiceKeyProvider.ToFts);
                var logger = serviceProvider.GetRequiredService<ILogger<Sender<PrismatTelegram>>>();
                return new Sender<PrismatTelegram>(socket!, ServiceKeyProvider.FromFts, toFtsProcessor!, logger);
            });

            services.AddHostedService(serviceProvider =>
            {
                var toFtsProcessor = serviceProvider.GetKeyedService<PrismatTelegramProcessor>(ServiceKeyProvider.ToFts);
                var logger = serviceProvider.GetRequiredService<ILogger<TelegramProvider>>();
                return new TelegramProvider(logger, toFtsProcessor!);
            });

            services.AddSingleton(serviceProvider =>
            {
                var fromFtsProcessor = serviceProvider.GetKeyedService<PrismatTelegramProcessor>(ServiceKeyProvider.FromFts);
                var server = serviceProvider.GetKeyedService<Client>(ServiceKeyProvider.FromFts);
                var logger = serviceProvider.GetRequiredService<ILogger<Receiver<PrismatTelegram>>>();
                return new Receiver<PrismatTelegram>(server!, ServiceKeyProvider.FromFts, fromFtsProcessor!, logger);
            });
        });
    var host = hostBuilder.Build();
    host.Run();
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
}