using CommandLine;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.KaeserSidecar.CmdOptions;
using Gebhardt.Acc.KaeserSidecar.Configuration;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

var initialLogger = InitialLogger.Create<Program>();

try
{
    IHostBuilder? hostBuilder = null;
    Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
        .WithParsed<SimulateOptions>(o =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .AddMassTransit(initialLogger)
                .AddKaeserSidecar(o)
                .ConfigureWebHost(h =>
                    h.UseKestrel()
                        .Configure(app =>
                        {
                            app.UseMetricServer();
                            app.UseDeveloperExceptionPage();
                        }));
        })
        .WithParsed<ProductionOptions>(_ =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .AddMassTransit(initialLogger)
                .AddKaeserSidecar(null)
                .ConfigureWebHost(h =>
                    h.UseKestrel()
                        .Configure(app =>
                        {
                            app.UseMetricServer();
                        }));
        });
    if (hostBuilder == null)
    {
        initialLogger.LogWarning("Invalid command line argument(s)");
        return 1;
    }
    var host = hostBuilder.Build();
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<KaeserSidecarDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    host.Run();
    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}
