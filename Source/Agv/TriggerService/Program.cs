using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.TriggerService.Configuration;
using Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess;
using Gebhardt.Core.Hosting.Configuration;
using Gebhardt.Core.WebApi.Extensions;
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
    var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddCoreConfiguration(context.HostingEnvironment.EnvironmentName);
        })
        .AddMassTransit(initialLogger)
        .AddTriggerService(initialLogger)
        .ConfigureServices((_, services) => services.AddRouting())
        .ConfigureWebHost(h =>
            h.UseKestrel()
                .Configure(app =>
                {
                    app.UseMetricServer();
                    app.UseDeveloperExceptionPage();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHealthProbes();
                    });
                })); 
    var host = hostBuilder.Build();
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TriggerServiceDbContext>();
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
