using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.PrismatCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
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
        .AddPrismatCommunication()
        .ConfigureWebHost(h =>
            h.UseKestrel()
                .Configure(app =>
                {
                    app.UseMetricServer();
                    app.UseDeveloperExceptionPage();
                }));

    var host = hostBuilder.Build();
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PrismatCommunicationDbContext>();
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
