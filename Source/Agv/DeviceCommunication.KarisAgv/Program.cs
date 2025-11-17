using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.DeviceCommunication.KarisAgv;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();

try
{
    var hostBuilder = DeviceCommunicationHostBuilder.CreateHostBuilder(args);

    var host = hostBuilder.Build();
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DeviceCommunicationDbContext>();
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

