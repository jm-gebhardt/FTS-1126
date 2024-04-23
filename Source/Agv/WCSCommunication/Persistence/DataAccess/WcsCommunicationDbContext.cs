using Gebhardt.Acc.WCSCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.WCSCommunication.Persistence.DataAccess.Configuration;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.WCSCommunication.Persistence.DataAccess;

/// <inheritdoc />
[PublicAPI]
public sealed class WcsCommunicationDbContext : DbContext
{
    /// <summary>
    /// Configuration section where the ConnectionString for AGV Master Control is located
    /// </summary>
    public const string ConfigSectionName = "AMC";

    /// <summary>
    /// Name of the migration table
    /// </summary>
    public const string MigrationTableName = $"__{nameof(WcsCommunicationDbContext)}MigrationHistory";

    /// <summary>
    /// Gets and sets the <see cref="HostOrder" /> DbSet.
    /// </summary>
    public DbSet<HostOrder> HostOrder { get; set; } = null!;

    /// <inheritdoc />
    public WcsCommunicationDbContext(DbContextOptions<WcsCommunicationDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new HostOrderEntityConfiguration());
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(c =>
        {
            // Logs from Microsoft.EntityFrameworkCore.Database.Command
            c.Log((RelationalEventId.CommandExecuted, LogLevel.Debug));
            // Logs from Microsoft.EntityFrameworkCore.Infrastructure
            c.Log((CoreEventId.ContextInitialized, LogLevel.Debug));

            c.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS);
        });
        base.OnConfiguring(optionsBuilder);
    }
}