using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();

try
{
    var hostBuilder = DeviceCommunicationVda5050HostBuilder.CreateHostBuilder(args, initialLogger);
    var host = hostBuilder.Build();
    if (!string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "SwaggerCli"))
    {
        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DeviceCommunicationVda5050DbContext>();
            await dbContext.Database.MigrateAsync();
        }

        host.Run();
    }

    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}