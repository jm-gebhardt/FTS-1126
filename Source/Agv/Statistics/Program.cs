using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Statistics.Configuration;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();

const string quartzSchedulerName = "MassTransit-Statistics";
const string quartzSchedulerId = "AUTO";

try
{
    var hostBuilder = StatisticsHostBuilder.CreateHostBuilder(args, initialLogger, quartzSchedulerId, quartzSchedulerName);

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