using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();
try
{
    var hostBuilder = DeviceCommunicationVda5050SimulatorHostBuilder.CreateHostBuilder(args, initialLogger);
    var host = hostBuilder.Build();
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<Vda5050SimulatorDbContext>();
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