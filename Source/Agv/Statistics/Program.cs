using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Logging;
using Gebhardt.StoreWare.Agv.Statistics.Configuration;
using Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.Statistics.WebApi;
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
        .AddMassTransit(initialLogger)
        .AddStatistics()
        .ConfigureWebHost(h =>
            h.UseKestrel()
                .AddStatisticsWebApi()
                .Configure(app =>
                {
                    app.UseMetricServer();
                    app.UseStatisticsWebApi();
                    app.UseDeveloperExceptionPage();
                }));

    var host = hostBuilder.Build();
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<StatisticsDbContext>();
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