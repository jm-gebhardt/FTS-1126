using DeviceCommunication.Vda5050.Simulator.Configuration;
using DeviceCommunication.Vda5050.Simulator.DataAccess;
using Gebhardt.Acc.Common.Logging;

var initialLogger = InitialLogger.Create<Program>();
try
{
		var host = Vda5050SimulatorHostBuilder.CreateHostBuilder(args).Build();
		using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Vda5050SimulatorDbContext>();
            await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(dbContext.Database);
        }
		host.Run();
		return 0;
}
catch (Exception ex)
{
		initialLogger.LogError(ex, "Unhandled exception");
		return 1;
}
