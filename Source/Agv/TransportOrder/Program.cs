using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;

try
{
    Parser.Default.ParseArguments<SimulateOptions>(args)
        .WithParsed(simulationOptions =>
        {
            using var host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => simulationOptions);
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                    {
                        x.AddSagaStateMachine<OrderStateMachine, OrderSaga>().InMemoryRepository();
                        x.AddConsumer<RoundRobinAssignAgvConsumer>();
                    });
                }).Build();
            host.Run();
        });
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

return 0;
