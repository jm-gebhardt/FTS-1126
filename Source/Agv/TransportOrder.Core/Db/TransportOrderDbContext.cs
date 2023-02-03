using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Db
{
    /// <summary>
    /// TransportOrder database context
    /// </summary>
    public class TransportOrderDbContext : DbContext
    {
        /// <summary>
        /// Configuration section where the ConnectionString is located
        /// </summary>
        public const string ConfigSectionName = "TO";

        /// <summary>
        /// DataSet "AgvAction"
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public DbSet<OrderAction> OrderAction { get; set; } = null!;


        /// <summary>
        /// Constructor with options
        /// </summary>
        /// <param name="options"></param>
        public TransportOrderDbContext(DbContextOptions<TransportOrderDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Adjust table schema
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderAction>().Property(p => p.OrderDetails).HasJsonConversion();

            modelBuilder.Entity<OrderAction>().Property(p => p.Nodes)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, System.Text.Json.JsonSerializerOptions.Default),
                    v => System.Text.Json.JsonSerializer.Deserialize<string[]>(v, System.Text.Json.JsonSerializerOptions.Default),
                    new ValueComparer<string[]>((c1, c2) =>
                        (c1 == null && c2 == null) ||
                        (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c));
        }

        /// <summary>
        /// Configure the DbContext
        /// The default EF Logging is very chatty and prints each CommandExecution using LogLevel.Information.
        /// This configuration changes this to LogLevel.Debug
        /// Source: https://github.com/dotnet/efcore/issues/14523#issuecomment-491532384
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(c =>
            {
                // Logs from Microsoft.EntityFrameworkCore.Database.Command
                c.Log((RelationalEventId.CommandExecuted, LogLevel.Debug));
                // Logs from Microsoft.EntityFrameworkCore.Infrastructure
                c.Log((CoreEventId.ContextInitialized, LogLevel.Debug));
            });
        }
    }
}
