using System.Reflection;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
    IHostBuilder? hostBuilder = null;

    Parser.Default.ParseArguments<ProductionOptions>(args)
        .WithParsed(_ =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ConcurrentQueue<KeyValuePair<Guid, Order>>>();
                    services.AddTransient<IAgvRegistry, EfAgvRegistry>();

                    services.AddDbContextFactory<SagaPersistenceDbContext>(builder =>
                    {
                        builder.UseSqlServer(context.Configuration.GetConnectionString(SagaPersistenceDbContext.ConfigSectionName), m =>
                        {
                            m.MigrationsHistoryTable(SagaPersistenceDbContext.MigrationTableName);
                        });
                    });

                    try
                    {
                        ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                        {
                            x.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                                .AddPersistence<OrderSaga, SagaPersistenceDbContext>();
                            x.AddSagaStateMachine<AgvStateMachine, AgvSaga>()
                                .AddPersistence<AgvSaga, SagaPersistenceDbContext>();
                            x.AddConsumer<AssignAgvConsumer>();
                            x.AddRequestClient<ReserveAgv>();
                        });
                    }
                    catch (InvalidOperationException e)
                    {
                        initialLogger.LogError(e, "MassTransit could not be started");
                    }
                });
        });

    // By using the generic overload of WithParsed above hostBuilder will be null for invalid command line arguments.
    if (hostBuilder != null)
    {
        hostBuilder.ConfigureWebHost(host =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            host.UseKestrel().ConfigureServices(services =>
            {
                services.AddMvc();

                services.AddControllers().AddApplicationPart(assembly);

                services.Configure<Microsoft.Extensions.Logging.EventLog.EventLogSettings>(settings =>
                {
                    settings.SourceName = assembly.GetName().Name ?? "WebService";
                });
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Monitoring API"
                    });

                    // Set the comments path for the Swagger JSON and UI.
                    string xmlFile = $"{assembly.GetName().Name}.xml";
                    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    options.IncludeXmlComments(xmlPath);
                });
            })
            .Configure(app =>
            {
                app.UseDefaultFiles(); // Serve Default.htm/html, Index.htm/html by default, without having to explicitly qualify the URL.
                app.UseStaticFiles(); // Allow html, images, etc. in wwwroot directory to be served. (use UseWebRoot to change folder)
                app.UseRouting();
                app.UseCors();
                app.UseAuthentication(); // verify user
                app.UseAuthorization(); // use security tokens

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

                app.UseSwagger();
                app.UseSwaggerUI();
            });
        });

        var host = hostBuilder.Build();

        using (var scope = host.Services.CreateScope())
        {
            // Apply migrations if SagaPersistenceDbContext is available
            var db = scope.ServiceProvider.GetService<SagaPersistenceDbContext>();
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
