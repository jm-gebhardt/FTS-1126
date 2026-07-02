using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;

/// <summary>
/// Db Context for PrismatCommunication
/// </summary>
public class PrismatCommunicationDbContext : BaseDbContext<PrismatCommunicationDbContext>, IDbContext<PrismatCommunicationDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "PRISMAT";

    /// <inheritdoc />
    public PrismatCommunicationDbContext(DbContextOptions<PrismatCommunicationDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new HostOrderEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SystemStatusEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramEntityConfiguration());
        modelBuilder.ApplyConfiguration(new HousekeepingConfigurationEntityConfiguration());
    }
}