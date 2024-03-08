using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.StoreWare.Agv.Common.Persistence.Ef;

/// <summary>
/// Provides a base class for design time db context factories. The dotnet ef tool automatically discovers
/// and uses an <see cref="IDesignTimeDbContextFactory{TContext}" /> when adding migrations for example.
/// </summary>
/// <typeparam name="TDbContext">The db context type.</typeparam>
public abstract class DesignTimeDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly string _connectionStringKey;
    private readonly string _migrationTableName;

    /// <summary>
    /// Constructs <see cref="DesignTimeDbContextFactory{TDbContext}" /> instances.
    /// </summary>
    /// <param name="connectionStringKey">The key to obtain the connection string from the environment or appsettings.</param>
    /// <param name="migrationTableName">The migration table name</param>
    protected DesignTimeDbContextFactory(string connectionStringKey, string migrationTableName)
    {
        _connectionStringKey = connectionStringKey;
        _migrationTableName = migrationTableName;
    }

    /// <inheritdoc />
    public TDbContext CreateDbContext(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args);
        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        hostBuilder.ConfigureHostConfiguration(builder => builder.SetBasePath(path)
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables());
        hostBuilder.ConfigureServices((ctx, services) =>
        {
            services.AddDbContextFactory<TDbContext>(o =>
            {
                o.UseSqlServer(ctx.Configuration.GetConnectionString(_connectionStringKey),
                    m => { m.MigrationsHistoryTable(_migrationTableName); });
            });
            services.AddMediator();
        });
        var host = hostBuilder.Build();

        var db = host.Services.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContext();
        return db;
    }
}