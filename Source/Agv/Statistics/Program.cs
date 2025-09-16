using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Statistics.Configuration;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Gebhardt.Acc.Statistics.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;

var initialLogger = InitialLogger.Create<Program>();

const string quartzSchedulerName = "MassTransit-Statistics";
const string quartzSchedulerId = "AUTO";

try
{
    var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
        .AddMassTransit(initialLogger)
        .AddStatistics()
        .ConfigureServices((_, services) =>
        {
            services.AddQuartz(q =>
            {
                q.SchedulerId = quartzSchedulerId;
                q.SchedulerName = quartzSchedulerName;
                q.UseTimeZoneConverter();
            });
            services.AddQuartzHostedService();
        })
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